using Monitel.Mal;
using Monitel.Mal.Providers;
using Monitel.Mal.Providers.Mal;
using OperInformApp.Foundation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;

namespace OperInformApp.ViewModel
{
    class AppViewModelBase : INotifyPropertyChanged
    {
        public AppViewModelBase() { ReadFileCon(); }
        #region Members
        public MalProvider DataProvider;
        public ModelImage mImage;
        private readonly string pathLog = @"C:\temp\OperInformApp.log";
        private readonly string pathCaon = @"C:\temp\OperInformApp_con.txt";
        public event PropertyChangedEventHandler PropertyChanged;

        private Guid _guidObg;
        public Guid GuidObj
        {
            get { return _guidObg; }
            set { _guidObg = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<String> _infoCollect = new ObservableCollection<String>();
        public ObservableCollection<string> InfoCollect
        {
            get { return _infoCollect; }
            set { _infoCollect = value; RaisePropertyChanged(); }
        }
        private string _OdbServerName;
        public string OdbServerName
        {
            get { return _OdbServerName; }
            set { _OdbServerName = value; RaisePropertyChanged(); }
        }
        
        private string _OdbInstanseName;
        public string OdbInstanseName
        {
            get { return _OdbInstanseName; }
            set { _OdbInstanseName = value; RaisePropertyChanged(); }
        }
        private int _OdbModelVersionId;
        public int OdbModelVersionId
        {
            get { return _OdbModelVersionId; }
            set { _OdbModelVersionId = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Commands



        /// <summary>
        /// Очистка протокола
        /// </summary>
        public ICommand ClearInfoCollect { get { return new RelayCommand(ClearInfoExecute, CanClear); } }
        bool CanClear() { return true; }
        void ClearInfoExecute() { InfoCollect.Clear(); }


        public void Connection()
        {
            try
            {
                MalContextParams context = new MalContextParams()
                {
                    OdbServerName = OdbServerName,
                    OdbInstanseName = OdbInstanseName,
                    OdbModelVersionId = OdbModelVersionId,
                };
                DataProvider = new MalProvider(context, MalContextMode.Open, "test");
                mImage = new ModelImage(DataProvider, true);
                Log("Подключение к ИМ выполнено! "); ;
            }
            catch { Log("Ошибка подключения! "); }
            SaveFileCon();
        }
        #endregion

        protected void SaveFileCon()
        {
            string text = $"Server11=;{OdbServerName};" +
                $"Instans11=;{OdbInstanseName};" +
                $"Model11=;{OdbModelVersionId};"+
                $"Guid=;{GuidObj};";
            using (FileStream fstream = new FileStream(pathCaon, FileMode.Create))
            {
                byte[] array = System.Text.Encoding.Default.GetBytes(text);
                fstream.Write(array, 0, array.Length);
            }
        }
        protected void ReadFileCon()
        {
            try
            {
                if (!Directory.Exists(@"C:\temp"))
                {
                    Directory.CreateDirectory(@"C:\temp");
                }
                using (FileStream fstream = File.OpenRead(pathCaon))
                {
                    byte[] array = new byte[fstream.Length];
                    fstream.Read(array, 0, array.Length);
                    string textFromFile = System.Text.Encoding.Default.GetString(array);
                    var range = textFromFile.Split(';');
                    try
                    {
                        OdbServerName = range[1];
                        OdbInstanseName = range[3];
                        OdbModelVersionId = Convert.ToInt32(range[5]);
                        GuidObj = new Guid(range[7]);
                    }
                    catch { };
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                OdbServerName = @"ag-lis-aipim";
                OdbInstanseName = @"ODB_SCADA";
                OdbModelVersionId = 2097;
                GuidObj = new Guid("F2DE26B1-9BFA-4E7A-999B-F111C1790991");
            }
        }
        public void Log(string message)
        {

            using (StreamWriter logFile = File.AppendText(pathLog))
            {
                InfoCollect.Insert(0, DateTime.Now.ToString("HH:mm:ss") + " " + message);
                logFile.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " " + message);
            }
        }
        public void RaisePropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public class ObservableRangeCollection<T> : ObservableCollection<T>
        {
            public void AddRange(IEnumerable<T> collection)
            {
                if (collection.Count()!=0)
                {
                    foreach (var i in collection)
                    {
                        Items.Add(i);
                    }
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, collection.ToList()));
                }                
            }
        }
        /// <summary>
        /// Для отображения инструментов фильтрации
        /// </summary>
        public class OppositeBooleanToVisibility : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (!(bool)value)
                {
                    return System.Windows.Visibility.Visible;
                }
                else
                {
                    return System.Windows.Visibility.Collapsed;
                }
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                System.Windows.Visibility visibility = (System.Windows.Visibility)value;

                return visibility == System.Windows.Visibility.Visible ? false : true;
            }
        }
    }
}
