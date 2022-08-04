using Monitel.Diogen.Core;
using Monitel.Diogen.Elements.Buttons;
using Monitel.Diogen.Elements.Charts;
using Monitel.Diogen.Elements.ClockIndicator;
using Monitel.Diogen.Elements.DevExpIndicators;
using Monitel.Diogen.Elements.Indicators;
using Monitel.Diogen.Elements.Tables;
using Monitel.Diogen.Elements.Violation;
using Monitel.Diogen.Infrastructure;
using Monitel.Mal;
using Monitel.Mal.Context.CIM16;
using Monitel.Mal.Providers;
using OperInformApp.Foundation;
using OperInformApp.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using DiogenDiagramCore = Monitel.Diogen.Core.DiogenDiagram;
using DiogenDiagramCiM = Monitel.Mal.Context.CIM16.DiogenDiagram;
using TableIndicator = Monitel.Diogen.Elements.Dashboards.TableIndicator;
using System.Threading;
using System.Windows.Threading;
using Monitel.Diogen.Elements.Requests;

namespace OperInformApp.ViewModel
{
    class AppViewModel : AppViewModelBase
    {
        private int _currentForm = 0;
        public int CurrentForm
        {
            get => _currentForm;
            set { _currentForm = value; RaisePropertyChanged(); }
        }
        private int _formsCount = 1;
        public int FormsCount
        {
            get => _formsCount;
            set { _formsCount = value; RaisePropertyChanged(); }
        }
       
        private string _path;
        public string Path
        {
            get => _path;
            set { _path = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<Link> _viewCollect = new ObservableCollection<Link>();
        public ObservableCollection<Link> ViewCollect
        {
            get { return _viewCollect; }
            set { _viewCollect = value; RaisePropertyChanged(); }
        }
        private ObservableCollection<Link> _exprCollect = new ObservableCollection<Link>();
        public ObservableCollection<Link> ExprCollect
        {
            get { return _exprCollect; }
            set { _exprCollect = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<TwoGuid> _guidsCollect = new ObservableCollection<TwoGuid>();
        public ObservableCollection<TwoGuid> GuidCollect
        {
            get { return _guidsCollect; }
            set { _guidsCollect = value; RaisePropertyChanged(); }
        }

        private int _index;
        private DataGridCellInfo _cellInfo;

        /// <summary>
        /// Текущее значение ячейки
        /// </summary>
        public DataGridCellInfo CellInfo
        {
            get { return _cellInfo; }
            set
            {
                _cellInfo = value;
                if (_cellInfo.Column != null)
                    _index = _cellInfo.Column.DisplayIndex;
            }
        }

        private Link _selectedLink;
        /// <summary>
        /// Выбранный эл-т 
        /// </summary>
        public Link SelectedLink
        {
            get { return _selectedLink; }
            set { _selectedLink = value; RaisePropertyChanged(); }
        }


        ObservableCollection<MeasValueDirectOperand> mvOperands { get; set; }
        ObservableCollection<PSRMeasValueOperand> psrOperands { get; set; }

        /// <summary>
        /// Функция поиска ответсвенного за моделирование
        /// </summary>
        Func<IdentifiedObject, ModelingAuthority> getMA = delegate (IdentifiedObject terminal)
        {
            ModelingAuthoritySet mas = terminal.ModelingAuthoritySet;
            IdentifiedObject parent = terminal.ParentObject;
            while (mas == null && parent != null)
            {
                mas = parent.ModelingAuthoritySet;
                parent = parent.ParentObject;
            }
            return mas.ModelingAuthority;
        };





        private CancellationTokenSource _tokenSource = new CancellationTokenSource();
        public ICommand LoadDataCommandAsync { get { return new RelayCommand(LoadExecute, CanLoadData); } }
        bool CanLoadData() { return mImage != null; }
        async void LoadExecute()
        {
            var progress = new Progress<int>();
            progress.ProgressChanged += ((sender, e) => { CurrentForm = e; });
            await LoadAsync(_tokenSource.Token, progress);
        }
        async Task LoadAsync(CancellationToken token = default, IProgress<int> progress = null)
        {
            ViewCollect.Clear();
            CurrentForm = 0;
            Mouse.OverrideCursor = Cursors.Wait;
            Log("Выполняется обработка форм ");

            mImage = new ModelImage(DataProvider, true);
            var obj = mImage.GetObject(GuidObj);
            SaveFileCon();
            MetaClass mdClass = mImage.MetaData.Classes["DiogenDiagram"];
            IEnumerable<DiogenDiagramCiM> mdCollect;

            ModelingAuthority modelingAuthority = mImage.GetObject(GuidObj) as ControlAreaOperator;
            if (modelingAuthority is null)
                mdCollect = mImage.GetObjects(mdClass).Cast<DiogenDiagramCiM>().Where(x => x == obj);
            else
            {
                mdCollect = mImage.GetObjects(mdClass).Cast<DiogenDiagramCiM>().Where(x => getMA(x) == modelingAuthority);
            }

            DiogenControl DC1 = new DiogenControl();

            var DD1 = DC1.Diagram;
            var formCounter = 0;

            foreach (DiogenDiagramCiM md in mdCollect)
            {
                FormsCount = mdCollect.ToList().Count;
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                try
                {
                    DD1.LoadFrom(md.image);
                }
                catch (Exception bex)
                {
                    Log("Ошибка в " + md.name + " Сообщение: " + bex.Message);
                }

                foreach (IDiogenElement element in DD1.ElementManager.Elements)
                {
                    var index = element.ZIndex;
                    ElementsCheck check = new ElementsCheck { MImage = mImage, NameForm = md.name, UidForm = md.Uid };
                    // try
                    //{
                    //Ячейка таблицы
                    if (element is TimeCell timeCell)
                    {
                        var links = check.CheckedTimeCell(timeCell);
                        foreach (var link in links)
                        {
                            link.IndexElement = index;
                            ViewCollect.Add(link);
                        }
                    }
                    //Таблица
                    else if (element is TimeTable timeTable)
                    {
                        if (timeTable.CustomData != null)
                        {
                            XElement expColold = new XElement("ExpressionCollection");
                            timeTable.CustomData.XLWrite(expColold);
                            var links = check.CheckExpressions(expColold, "\r\nТаблица [" + index + "]");
                            foreach (var link in links)
                            {
                                link.IndexElement = index;
                                ViewCollect.Add(link);
                            }
                        }
                    }
                    //График
                    else if (element is SimpleChartView chart)
                    {
                        var links = check.CheckedChart(chart);
                        foreach (var link in links)
                        {
                            ViewCollect.Add(link);
                        }
                        if (chart.CustomData != null)
                        {
                            XElement expColold = new XElement("ExpressionCollection");
                            chart.CustomData.XLWrite(expColold);
                            var linksFormuls = check.CheckExpressions(expColold, "\r\nГрафик [" + index + "]");
                            foreach (var link in linksFormuls)
                            {
                                link.IndexElement = index;
                                ViewCollect.Add(link);
                            }
                        }
                    }
                    //Кнопка дистанционного управления
                    else if (element is TelecontrolButton tButton)
                    {
                        var links = check.CheckedObjButton(tButton);
                        foreach (var link in links)
                        {
                            link.IndexElement = index;
                            ViewCollect.Add(link);
                        }
                    }
                    //Кнопка ручного ввода
                    else if (element is HandControlButton hButton)
                    {
                        var links = check.CheckedObjButton(hButton);
                        foreach (var link in links)
                        {
                            link.IndexElement = index;
                            ViewCollect.Add(link);
                        }
                    }
                    //Индикатор заявок
                    else if (element is ZVKInCell zIndicator)
                    {
                        var links = check.CheckedObjIndicator(zIndicator);
                        foreach (var link in links)
                        {
                            link.IndexElement = index;
                            ViewCollect.Add(link);
                        }
                    }
                    //Табличный индикатор
                    else if (element is TableIndicator tableIndicator)
                    {
                        var links = check.CheckedTableIndicator(tableIndicator);
                        foreach (var link in links)
                        {
                            link.IndexElement = index;
                            ViewCollect.Add(link);
                        }
                        if (tableIndicator.CustomData != null)
                        {
                            XElement expColold = new XElement("ExpressionCollection");
                            tableIndicator.CustomData.XLWrite(expColold);
                            var linksFormuls = check.CheckExpressions(expColold, "\r\nИндикатор [" + index + "]");
                            foreach (var link in linksFormuls)
                            {
                                link.IndexElement = index;
                                ViewCollect.Add(link);
                            }
                        }
                    }
                    //Составной индикатор
                    else if (element is CompositeIndicator compIndicator)
                    {
                        var links = check.CheckedIndicator(compIndicator);
                        foreach (var link in links)
                        {
                            link.IndexElement = index;
                            ViewCollect.Add(link);
                        }
                        if (compIndicator.CustomData != null)
                        {
                            XElement expColold = new XElement("ExpressionCollection");
                            compIndicator.CustomData.XLWrite(expColold);
                            var linksFormuls = check.CheckExpressions(expColold, "\r\nИндикатор[" + index + "]");
                            foreach (var link in linksFormuls)
                            {
                                link.IndexElement = index;
                                ViewCollect.Add(link);
                            }
                        }
                    }
                    //Часы                       
                    else if (element is Clock cIndicator)
                    {
                        var links = check.CheckedObjIndicator(cIndicator);
                        foreach (var link in links)
                        {
                            link.IndexElement = index;
                            ViewCollect.Add(link);
                        }
                    }
                    //Индикатор погодного явления
                    else if (element is WeatherConditionIndicator weathIndicator)
                    {
                        var links = check.CheckedObjIndicator(weathIndicator);
                        foreach (var link in links)
                        {
                            link.IndexElement = index;
                            ViewCollect.Add(link);
                        }
                    }
                    //Стрелка
                    else if (element is ArrowIndicator aIndicator)
                    {
                        var links = check.CheckedArrowIndicator(aIndicator);
                        foreach (var link in links)
                        {
                            link.IndexElement = index;
                            ViewCollect.Add(link);
                        }
                        if (aIndicator.CustomData != null)
                        {
                            XElement expColold = new XElement("ExpressionCollection");
                            aIndicator.CustomData.XLWrite(expColold);
                            try
                            {
                                var linksFormuls = check.CheckExpressions(expColold, "\r\nСтрелка [" + index + "]");
                                foreach (var link in linksFormuls)
                                {
                                    link.IndexElement = index;
                                    ViewCollect.Add(link);
                                }
                            }
                            catch (NullReferenceException) { }
                        }
                    }
                    //Комплексный индикатор
                    else if (element is ComplexIndicator complIndicator)
                    {
                        var links = check.CheckedIndicator(complIndicator);
                        foreach (var link in links)
                        {
                            ViewCollect.Add(link);
                        }
                        if (complIndicator.CustomData != null)
                        {
                            XElement expColold = new XElement("ExpressionCollection");
                            complIndicator.CustomData.XLWrite(expColold);
                            var linksFormuls = check.CheckExpressions(expColold, "\r\nИндикатор [" + index + "]");
                            foreach (var link in linksFormuls)
                            {
                                link.IndexElement = index;
                                ViewCollect.Add(link);
                            }
                        }
                    }
                    //Линейный индикатор
                    else if (element is LinearIndicator linIndicator)
                    {
                        var links = check.CheckedDevIndicator(linIndicator);
                        foreach (var link in links)
                        {
                            link.IndexElement = index;
                            ViewCollect.Add(link);
                        }
                    }
                    //Круговой индикатор
                    else if (element is CircularIndicator cirIndicator)
                    {
                        var links = check.CheckedDevIndicator(cirIndicator);
                        foreach (var link in links)
                        {
                            link.IndexElement = index;
                            ViewCollect.Add(link);
                        }
                    }
                    //Индикатор
                    else if (element is Indicator indicator)
                    {
                        var links = check.CheckedIndicator(indicator);
                        foreach (var link in links)
                        {
                            link.IndexElement = index;
                            ViewCollect.Add(link);
                        }
                        if (indicator.CustomData != null)
                        {
                            XElement expColold = new XElement("ExpressionCollection");
                            indicator.CustomData.XLWrite(expColold);
                            try
                            {
                                var linksFormuls = check.CheckExpressions(expColold, "\r\nИндикатор [" + index + "]");
                                foreach (var link in linksFormuls)
                                {
                                    link.IndexElement = index;
                                    ViewCollect.Add(link);
                                }
                            }
                            catch (NullReferenceException) { }
                            catch (Exception e)
                            {
                                Log($"Ошибка в {md.name} элемент [{index}] Сообщение: " + e.Message);
                            }
                        }
                    }
                    //Индикатор
                    else if (element is UniversalIndicator unIndicator)
                    {
                        var links = check.CheckedIndicator(unIndicator);
                        foreach (var link in links)
                        {
                            link.IndexElement = index;
                            ViewCollect.Add(link);
                        }
                        if (unIndicator.CustomData != null)
                        {
                            XElement expColold = new XElement("ExpressionCollection");
                            unIndicator.CustomData.XLWrite(expColold);
                            try
                            {
                                var linksFormuls = check.CheckExpressions(expColold, "\r\nИндикатор [" + index + "]");
                                foreach (var link in linksFormuls)
                                {
                                    link.IndexElement = index;
                                    ViewCollect.Add(link);
                                }
                            }
                            catch (NullReferenceException) { }
                        }
                    }
                    /* else
                     {
                         var link = new FormsLink { Info = element.GetType().ToString(), NameForm = md.name, UidForm = md.Uid };
                         ViewCollect.Add(link);
                     }*/

                    //}
                    /*catch (NullReferenceException e) //(Exception bex)
                    {
                        Log($"Ошибка в {md.name} элемент [{index}] Сообщение: " + e.Message);
                    }*/
                    /*catch (NotSupportedException e) //(Exception bex)
                    {
                        Log($"Ошибка в {md.name} элемент [{index}] Сообщение: " + e.Message);
                    }*/
                }
                formCounter++;
                progress?.Report(formCounter);
            }
            Mouse.OverrideCursor = null;
            Log("Обработка форм выполнена");


        }


        public ICommand CorrectLinkCommand { get { return new RelayCommand(CorrectLinkExecute, CanCorrectLink); } }
        bool CanCorrectLink() { return GuidCollect.Count() != 0 && ExprCollect.Count() != 0 && ViewCollect.Count()!=0 ? true : false; }
        void CorrectLinkExecute()
		{   

            foreach(var item in GuidCollect)
			{
                try
				{
                    var oldValue =(MeasurementValue) mImage.GetObject(item.OldGuid);
                    var newValue = (MeasurementValue)mImage.GetObject(item.NewGuid);
                    var views = ViewCollect.Where(x => x.UidOI == oldValue.Uid);
                    var formulas = ExprCollect.Where(x => x.UidOI == oldValue.Uid);
                    foreach(var view in views)
					{
                        var obj = mImage.GetObject(view.UidForm);
                        if (obj is DiogenDiagramCiM md)
						{
                            CorrectLink corrector = new CorrectLink { MImage = mImage, NameForm = md.name, UidForm = md.Uid };
                            DiogenControl DC1 = new DiogenControl();
                            var DD1 = DC1.Diagram;
                            try
                            {
                                DD1.LoadFrom(md.image);
                            }
                            catch (Exception bex)
                            {
                                Log("Ошибка в " + md.name + " Сообщение: " + bex.Message);
                            }

                            var element = DD1.ElementManager.Elements.Single(x => x.ZIndex == view.IndexElement);
                            if (element is TimeCell timeCell)
                            {
                                //corrector.CorrectTimeCell(timeCell, newValue);
                            }
                            else if (element is TimeTable timeTable) { }
                            else if (element is SimpleChartView chart) { }
                            else if (element is TelecontrolButton tButton) { }
                            else if (element is HandControlButton hButton) { }
                            else if (element is ZVKInCell zIndicator) { }
                            else if (element is TableIndicator tableIndicator) { }
                            else if (element is CompositeIndicator compIndicator) { }
                            else if (element is Clock cIndicator) { }
                            else if (element is WeatherConditionIndicator weathIndicator) { }
                            else if (element is ArrowIndicator aIndicator) { }
                            else if (element is ComplexIndicator complIndicator) { }
                            else if (element is LinearIndicator linIndicator) { }
                            else if (element is CircularIndicator cirIndicator) { }
                            else if (element is Indicator indicator) { }
                            else if (element is UniversalIndicator unIndicator) { }


                        }
                        
                    }
                    foreach(var formula in formulas)
					{

					}
                }
                catch(Exception ex)
				{
                    Log("Ошибка поиска значения. Проверьте Uid");
				}                
                

			}

		}
        /// <summary>
        /// Подключение к ИМ
        /// </summary>
        public ICommand ConnectCommand { get { return new RelayCommand(ConnectExecute); } }
        void ConnectExecute()
        {
            ExprCollect.Clear();
            Mouse.SetCursor(Cursors.Wait);
            Connection();
            if (mImage!=null)
			{
                MetaClass mvClass = mImage.MetaData.Classes["MeasValueDirectOperand"];
                IEnumerable<MeasValueDirectOperand> mvCollect = mImage.GetObjects(mvClass).Cast<MeasValueDirectOperand>();
                mvOperands = new ObservableCollection<MeasValueDirectOperand>(mvCollect);

                MetaClass psrClass = mImage.MetaData.Classes["PSRMeasValueOperand"];
                IEnumerable<PSRMeasValueOperand> psrCollect = mImage.GetObjects(psrClass).Cast<PSRMeasValueOperand>();
                psrOperands = new ObservableCollection<PSRMeasValueOperand>(psrCollect.Where(x => x.Expression is MeasurementExpression));
                //mvOperands = (ObservableCollection<MeasValueOperand>)allOperands.Where(x => x.Expression is MeasurementExpression);
                CheckExpressions();
            }         
            Mouse.SetCursor(Cursors.AppStarting);
        }

        public ICommand CopySelectedLink { get { return new RelayCommand(StartSelectCheckInfo); } }
        bool CanSelectCheckInfo() { return _index == 1 && _index == 4 ? true : false; }
        void StartSelectCheckInfo()
        {
            switch (_index)
            {
                case 1:
                    Clipboard.SetText(SelectedLink.UidForm.ToString());
                    break;
                case 4:
                    Clipboard.SetText(SelectedLink.UidOI.ToString());
                    break;
            }
        }

        public void CheckExpressions()
        {
            foreach (var mvOperand in mvOperands)
            {
                var mv = (MeasurementValue)mImage.GetObject(mvOperand.MeasurementValue.Uid);


                if (mv != null)
                {
                    var GVClass = mImage.MetaData.Classes.First(x => x.Id == mvOperand.MeasurementValue.ClassId);
                    ExprCollect.Add(new Link
                    {
                        ClassOI = GVClass.DisplayName,
                        NameForm = mvOperand.Expression.name,
                        UidForm = mvOperand.Expression.Uid,
                        NameOI = mv.name,
                        UidOI = mv.Uid,
                        TypeLink = Link.LinkEnum.DirectLink,
                        Info = $"Операнд: {mvOperand.name}"
                    });
                }
                else
                {
                    ExprCollect.Add(new Link
                    {
                        NameForm = mvOperand.Expression.name,
                        UidForm = mvOperand.Expression.Uid,
                        //NameOI = mv.name,
                        UidOI = Guid.Empty,
                        TypeLink = Link.LinkEnum.ErrorDirectLink,
                        Info = $"Операнд: {mvOperand.name}"
                    });
                }

            }
            foreach (var psrOperand in psrOperands)
            {
                if (psrOperand.Uid==new Guid("EF3472C1-F80F-4F88-B01E-81AFE324FC75"))
				{
                    var test = 0;
				}
                var PowerSystemResourceGuid = psrOperand.PowerSystemResource;
                var MeasTypeGuid = psrOperand.MeasurementType;
                var ValueTypeGuid = psrOperand.MeasurementValueType;
                var terminal = psrOperand.Terminal;
                var phase = psrOperand.phases;
                IndirectLinkMeas link = new IndirectLinkMeas
                {
                    PowerSystemResourceGuid = psrOperand.PowerSystemResource.Uid,
                    MeasTypeGuid = psrOperand.MeasurementType.Uid,
                    ValueTypeGuid = psrOperand.MeasurementValueType.Uid
                };
                if (psrOperand.phases != null)
                {
                    link.SpecifiedPhaseCode = (MeasurementPhaseCode)psrOperand.phases;
                    link.SpecifiedPhaseStr = psrOperand.phases.ToString();
                }
                if (psrOperand.Terminal != null)
                {
                    link.SpecifiedTerminal = psrOperand.Terminal.Uid;
                }
                var measUid = GetIndirectLink(link);
                var mvOperand = (MeasurementValue)mImage.GetObject(measUid);
                if (mvOperand != null)
                {
                    var GVClass = mImage.MetaData.Classes.First(x => x.Id == mvOperand.ClassId);
                    ExprCollect.Add(new Link
                    {
                        ClassOI = GVClass.DisplayName,
                        NameForm = psrOperand.Expression.name,
                        UidForm = psrOperand.Expression.Uid,
                        NameOI = mvOperand.name,
                        UidOI = mvOperand.Uid,
                        TypeLink = Link.LinkEnum.IndirectLink,
                        Info = $"Операнд: {psrOperand.name}"
                    });
                }
                else
                {
                    ExprCollect.Add(new Link
                    {
                        NameForm = psrOperand.Expression.name,
                        UidForm = psrOperand.Expression.Uid,
                        //NameOI = psrOperand.name,
                        UidOI = Guid.Empty,
                        TypeLink = Link.LinkEnum.ErrorIndirectLink,
                        Info = $"Операнд: {psrOperand.name}"

                    });
                }

            }
        }
        private Guid GetIndirectLink(IndirectLinkMeas link)
        {
            var obj = mImage.GetObject(link.PowerSystemResourceGuid);
            if (obj.GetType().ToString().Contains("Terminal"))
            {
                Terminal tm = (Terminal)obj;
                link.PowerSystemResourceGuid = tm.ConductingEquipment.Uid;
                obj = mImage.GetObject(link.PowerSystemResourceGuid);
                link.SpecifiedTerminal = tm.Uid;
            }
            PowerSystemResource psr = (PowerSystemResource)obj;
            Measurement meas = null;//=psr.Measurements.FirstOrDefault(x => x.MeasurementType.Uid == link.MeasTypeGuid);
            if (link.SpecifiedPhaseStr != null && link.SpecifiedTerminal == Guid.Empty)
            {
                meas = psr.Measurements.FirstOrDefault(x => x.MeasurementType.Uid == link.MeasTypeGuid && x.phases.ToString() == link.SpecifiedPhaseStr);
            }
            else if (link.SpecifiedPhaseStr == null && link.SpecifiedTerminal != Guid.Empty)
            {
                meas = psr.Measurements.FirstOrDefault(x => x.MeasurementType.Uid == link.MeasTypeGuid && x.Terminal != null && x.Terminal.Uid == link.SpecifiedTerminal);
            }
            else if (link.SpecifiedPhaseStr != null && link.SpecifiedTerminal != Guid.Empty)
            {
                meas = psr.Measurements.FirstOrDefault(x => x.MeasurementType.Uid == link.MeasTypeGuid && x.Terminal != null && x.Terminal.Uid == link.SpecifiedTerminal && x.phases.ToString() == link.SpecifiedPhaseStr);
            }
            else if (link.SpecifiedTerminal == Guid.Empty && link.SpecifiedPhaseStr == null)
            {
                meas = psr.Measurements.FirstOrDefault(x => x.MeasurementType.Uid == link.MeasTypeGuid && x.Terminal == null && x.phases == null);
                if (meas == null)
                {
                    meas = psr.Measurements.FirstOrDefault(x => x.MeasurementType.Uid == link.MeasTypeGuid);
                }
            }
            if (meas == null)
            { return Guid.Empty; }
            else if (meas.GetType().ToString().Contains("Analog"))
            {
                Analog a = (Analog)meas;
                var av = a.AnalogValues.FirstOrDefault(x => x.MeasurementValueType.Uid == link.ValueTypeGuid);
                if (av != null)
                    return av.Uid;
                else
                    return Guid.Empty;
            }
            else if (meas.GetType().ToString().Contains("Discrete"))
            {
                Discrete d = (Discrete)meas;
                var dv = d.DiscreteValues.FirstOrDefault(x => x.MeasurementValueType.Uid == link.ValueTypeGuid);
                if (dv != null)
                    return dv.Uid;
                else
                    return Guid.Empty;
            }
            return Guid.Empty;
        }


    }
}
