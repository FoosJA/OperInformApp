using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monitel.Diogen.Elements.Tables;
using Monitel.Mal;
using Monitel.Diogen.Infrastructure;
using Monitel.Mal.Context.CIM16;
using Monitel.Diogen.Elements.BasicElements;
using System.Xml.Linq;
using Monitel.Diogen.Elements.Indicators;
using Monitel.Diogen.Elements.Charts;
using Monitel.Diogen.Elements.ClockIndicator;
using Monitel.Diogen.Elements.Violation;
using Monitel.Diogen.Elements.Buttons;
using Monitel.Diogen.Elements.DevExpIndicators;
using OperInformApp.Model;
using LinkEnum = OperInformApp.Model.Link.LinkEnum;
using static OperInformApp.ViewModel.AppViewModelBase;
using TableIndicator = Monitel.Diogen.Elements.Dashboards.TableIndicator;
using Monitel.Diogen.Elements.Requests;

namespace OperInformApp.Foundation
{
    class ElementsCheck
    {
        public ModelImage MImage { get; set; }
        public Guid UidForm { get; set; }
        public string NameForm { get; set; }

        /// <summary>
        /// Проверка ячеек таблиц
        /// </summary>
        /// <param name="timeCell"></param>
        /// <returns></returns>
        public ObservableCollection<Link> CheckedTimeCell(TimeCell timeCell)
        {
            ObservableRangeCollection<Link> LinkCollect = new ObservableRangeCollection<Link>();
            string information = $"Ячейка ({timeCell.Row};{timeCell.Column}) [{timeCell.ZIndex}]";
            if (timeCell.IsLinked)
            {
                Guid elementGuid = timeCell.ElementGuid;
                Guid measurementType = timeCell.MeasurementType;
                Guid measurementValueType = timeCell.MeasurementValueType;
                var obj = MImage.GetObject(elementGuid);
                if (obj == null)
                {
                    LinkCollect.Add(new Link
                    {
                        TypeLink = LinkEnum.ErrorIndirectLink,
                        Info = information,
                        UidForm = UidForm,
                        NameForm = NameForm
                    });
                }
                else if (obj is MeasurementValue measValue)
                {
                    LinkCollect.Add(new Link
                    {
                        UidOI = measValue.Uid,
                        NameOI = measValue.name,
                        ClassOI = MImage.MetaData.Classes.First(x => x.Id == measValue.ClassId).DisplayName,
                        TypeLink = LinkEnum.DirectLink,
                        Info = information,
                        UidForm = UidForm,
                        NameForm = NameForm
                    });
                }
                else if (obj is Analog analog)
                {
                    try
                    {
                        AnalogValue av = analog.AnalogValues.FirstOrDefault(x => x.MeasurementValueType.Uid == measurementValueType);
                        LinkCollect.Add(new Link
                        {
                            UidOI = av.Uid,
                            NameOI = av.name,
                            ClassOI = MImage.MetaData.Classes.First(x => x.Id == av.ClassId).DisplayName,
                            TypeLink = LinkEnum.IndirectLink,
                            Info = information,
                            UidForm = UidForm,
                            NameForm = NameForm
                        });
                    }
                    catch
                    {
                        LinkCollect.Add(new Link
                        {
                            TypeLink = LinkEnum.ErrorIndirectLink,
                            Info = information,
                            UidForm = UidForm,
                            NameForm = NameForm
                        });
                    }
                }
                else if (obj is Discrete discrete)
                {
                    try
                    {
                        DiscreteValue dv = discrete.DiscreteValues.FirstOrDefault(x => x.MeasurementValueType.Uid == measurementValueType);
                        LinkCollect.Add(new Link
                        {
                            UidOI = dv.Uid,
                            NameOI = dv.name,
                            ClassOI = MImage.MetaData.Classes.First(x => x.Id == dv.ClassId).DisplayName,
                            TypeLink = LinkEnum.IndirectLink,
                            Info = information,
                            UidForm = UidForm,
                            NameForm = NameForm
                        });
                    }
                    catch
                    {
                        LinkCollect.Add(new Link
                        {
                            TypeLink = LinkEnum.ErrorIndirectLink,
                            Info = information,
                            UidForm = UidForm,
                            NameForm = NameForm
                        });
                    }
                }
                else if (elementGuid != Guid.Empty && measurementType != Guid.Empty && measurementValueType != Guid.Empty)
                {
                    Guid measUid = Guid.Empty;
                    IndirectLinkMeas link = new IndirectLinkMeas
                    {
                        PowerSystemResourceGuid = elementGuid,
                        MeasTypeGuid = measurementType,
                        ValueTypeGuid = measurementValueType
                    };
                    if (timeCell.PhaseCode != null)
                    {
                        link.SpecifiedPhaseCode = (MeasurementPhaseCode)timeCell.PhaseCode;
                        link.SpecifiedPhaseStr = timeCell.PhaseCode.ToString();
                    }
                    measUid = GetIndirectLink(link);
                    if (measUid != Guid.Empty)
                    {
                        MeasurementValue measVal = (MeasurementValue)MImage.GetObject(measUid);
                        LinkCollect.Add(new Link
                        {
                            UidOI = measVal.Uid,
                            NameOI = measVal.name,
                            ClassOI = MImage.MetaData.Classes.First(x => x.Id == measVal.ClassId).DisplayName,
                            TypeLink = LinkEnum.IndirectLink,
                            Info = information,
                            UidForm = UidForm,
                            NameForm = NameForm
                        });
                    }
                    else
                    {
                        LinkCollect.Add(new Link
                        {
                            TypeLink = LinkEnum.ErrorIndirectLink,
                            Info = information,
                            UidForm = UidForm,
                            NameForm = NameForm
                        });
                    }
                }
            }
            else if (timeCell.HasLocalExpressions)
            {
                XElement expColold = new XElement("ExpressionCollection");
                timeCell.CustomData.XLWrite(expColold);
                LinkCollect.AddRange(CheckExpressions(expColold, information));
            }
            return LinkCollect;
        }

        public ObservableCollection<Link> CheckedChart(SimpleChartView chart)
        {
            var index = chart.ZIndex;
            string inform = "Объект графика [" + index + "]";
            ObservableRangeCollection<Link> LinkCollect = new ObservableRangeCollection<Link>();
            var obj = MImage.GetObject(chart.ElementGuid);
            if (obj == null) { }
            else if (obj is MeasurementValue measVal)
            {
                LinkCollect.Add(new Link
                {
                    NameForm = NameForm,
                    UidForm = UidForm,
                    NameOI = measVal.name,
                    ClassOI = MImage.MetaData.Classes.First(x => x.Id == measVal.ClassId).DisplayName,
                    UidOI = measVal.Uid,
                    TypeLink = LinkEnum.DirectLink,
                    Info = inform,
                });
            }
            foreach (var item in chart.Items)
            {
                string information = $"График [{index}] {item.ItemName}";
                SecondLevelBase chartItem = (SecondLevelBase)item;
                if (chartItem.ElementGuid != Guid.Empty)
                    LinkCollect.Add(CheckedItem(chartItem, information));
            }

            return LinkCollect;
        }
        public ObservableCollection<Link> CheckedTableIndicator(TableIndicator tableIndicator)
        {
            var index = tableIndicator.ZIndex;
            string inform = "Объект индикатора [" + index + "]";
            ObservableRangeCollection<Link> LinkCollect = new ObservableRangeCollection<Link>();
            var obj = MImage.GetObject(tableIndicator.ElementGuid);
            if (obj == null) { }
            else if (obj is MeasurementValue measVal)
            {
                LinkCollect.Add(new Link
                {
                    NameForm = NameForm,
                    UidForm = UidForm,
                    NameOI = measVal.name,
                    ClassOI = MImage.MetaData.Classes.First(x => x.Id == measVal.ClassId).DisplayName,
                    UidOI = measVal.Uid,
                    TypeLink = LinkEnum.DirectLink,
                    Info = inform,
                });
            }
            foreach (var item in tableIndicator.Items)
            {
                string information = $"Индикатор [{index}]\r\n{item.ItemName}";
                if (item.GetType().ToString().Contains("TableIndicatorItem") && item.ElementGuid != Guid.Empty)
                {
                    try
                    {
                        MeasurementValue measValue = (MeasurementValue)MImage.GetObject(item.ElementGuid);
                        LinkCollect.Add(new Link
                        {
                            UidOI = measValue.Uid,
                            NameOI = measValue.name,
                            ClassOI = MImage.MetaData.Classes.First(x => x.Id == measValue.ClassId).DisplayName,
                            TypeLink = LinkEnum.DirectLink,
                            Info = information,
                            UidForm = UidForm,
                            NameForm = NameForm
                        });
                    }
                    catch
                    {
                        LinkCollect.Add(new Link
                        {
                            TypeLink = LinkEnum.DirectLink,
                            Info = information,
                            UidForm = UidForm,
                            NameForm = NameForm
                        });
                    }
                }
                else if (item is IdentifiedIndicatorItem idItem)
                {
                    if (item.ElementGuid != Guid.Empty)
                        LinkCollect.Add(CheckedItem(idItem, information));
                }
            }
            return LinkCollect;
        }

        public ObservableCollection<Link> CheckedIndicator(IndicatorBase indicator)
        {
            string information = "Объект индикатора [" + indicator.ZIndex + "]";
            ObservableRangeCollection<Link> LinkCollect = new ObservableRangeCollection<Link>();
            var obj = MImage.GetObject(indicator.ElementGuid);
            if (obj == null) { }
            else if (obj is MeasurementValue measVal)
            {
                LinkCollect.Add(new Link
                {
                    NameForm = NameForm,
                    UidForm = UidForm,
                    NameOI = measVal.name,
                    ClassOI = MImage.MetaData.Classes.First(x => x.Id == measVal.ClassId).DisplayName,
                    UidOI = measVal.Uid,
                    TypeLink = LinkEnum.DirectLink,
                    Info = information
                });
            }
            else if (obj is Terminal || obj is PowerSystemResource)
            {
                Guid measUid = Guid.Empty;
                IndirectLinkMeas link = new IndirectLinkMeas
                {
                    PowerSystemResourceGuid = indicator.ElementGuid,
                    MeasTypeGuid = indicator.MeasurementTypeUid,
                    ValueTypeGuid = indicator.MeasurementValueTypeUid
                };
                if (indicator.PhaseCode != null)
                {
                    link.SpecifiedPhaseCode = (MeasurementPhaseCode)indicator.PhaseCode;
                    link.SpecifiedPhaseStr = indicator.PhaseCode.ToString();
                }
                measUid = GetIndirectLink(link);
                if (measUid != Guid.Empty)
                {
                    MeasurementValue measValue = (MeasurementValue)MImage.GetObject(measUid);
                    LinkCollect.Add(new Link
                    {
                        UidOI = measValue.Uid,
                        NameOI = measValue.name,
                        ClassOI = MImage.MetaData.Classes.First(x => x.Id == measValue.ClassId).DisplayName,
                        TypeLink = LinkEnum.IndirectLink,
                        Info = information,
                        UidForm = UidForm,
                        NameForm = NameForm
                    });
                }
                else
                {
                    LinkCollect.Add(new Link
                    {
                        TypeLink = LinkEnum.ErrorIndirectLink,
                        Info = information,
                        UidForm = UidForm,
                        NameForm = NameForm
                    });
                }
            }
            /*else if (indicator.MeasurementValueTypeUid != Guid.Empty && indicator.MeasurementTypeUid!= Guid.Empty)
			{

			}*/
            foreach (var item in indicator.Items)
            {
                information += "\r\n" + item.ItemName;
                IdentifiedIndicatorItem idItem = (IdentifiedIndicatorItem)item;
                if (item.ElementGuid != Guid.Empty)
                    LinkCollect.Add(CheckedItem(idItem, information));

            }
            return LinkCollect;
        }
        public ObservableCollection<Link> CheckedDevIndicator(LinearIndicator indicator)
        {
            string information = "Объект индикатора [" + indicator.ZIndex + "]";
            ObservableRangeCollection<Link> LinkCollect = new ObservableRangeCollection<Link>();
            var obj = MImage.GetObject(indicator.ElementGuid);
            if (obj == null) { }
            else if (obj is MeasurementValue measVal)
            {
                var objClass = MImage.MetaData.Classes.First(x => x.Id == measVal.ClassId);
                LinkCollect.Add(new Link
                {
                    NameForm = NameForm,
                    UidForm = UidForm,
                    NameOI = measVal.name,
                    ClassOI = objClass.DisplayName,
                    UidOI = measVal.Uid,
                    TypeLink = LinkEnum.DirectLink,
                    Info = information
                });
            }
            foreach (var item in indicator.Items)
            {
                string Info = "Индикатор [" + indicator.ZIndex + "]\r\n" + item.ItemName;
                IndicatorItem idItem = (IndicatorItem)item;
                if (item.ElementGuid != Guid.Empty)
                    LinkCollect.Add(CheckedItem(idItem, Info));
            }
            return LinkCollect;
        }
        private Link CheckedItem(IndicatorItem item, string information)
        {
            Link link = new Link();
            Guid elementGuid = item.ElementGuid;
            var objItem = MImage.GetObject(elementGuid);
            if (objItem == null)
            {
                link = new Link
                {
                    TypeLink = LinkEnum.ErrorDirectLink,
                    Info = information,
                    UidForm = UidForm,
                    NameForm = NameForm
                };
            }
            else if (objItem is MeasurementValue measValue)
            {
                var GVClass = MImage.MetaData.Classes.First(x => x.Id == measValue.ClassId);
                link = new Link
                {
                    UidOI = measValue.Uid,
                    NameOI = measValue.name,
                    ClassOI = GVClass.DisplayName,
                    TypeLink = LinkEnum.DirectLink,
                    Info = information,
                    UidForm = UidForm,
                    NameForm = NameForm
                };
            }
            return link;
        }
        private Link CheckedItem(IdentifiedIndicatorItem item, string information)
        {

            Link link = new Link();
            Guid elementGuid = item.ElementGuid;
            Guid measurementType = item.MeasurementType;
            Guid measurementValueType = item.MeasurementValueType;

            var objItem = MImage.GetObject(elementGuid);
            if (objItem == null)
            {
                link = new Link
                {
                    TypeLink = LinkEnum.ErrorDirectLink,
                    Info = information,
                    UidForm = UidForm,
                    NameForm = NameForm
                };
            }
            else if (objItem is MeasurementValue measValue)
            {
                link = new Link
                {
                    UidOI = measValue.Uid,
                    NameOI = measValue.name,
                    ClassOI = MImage.MetaData.Classes.First(x => x.Id == measValue.ClassId).DisplayName,
                    TypeLink = LinkEnum.DirectLink,
                    Info = information,
                    UidForm = UidForm,
                    NameForm = NameForm
                };
            }
            else if (objItem is Analog analog)
            {
                try
                {
                    AnalogValue av = analog.AnalogValues.FirstOrDefault(x => x.MeasurementValueType.Uid == measurementValueType);
                    link = new Link
                    {
                        UidOI = av.Uid,
                        NameOI = av.name,
                        ClassOI = MImage.MetaData.Classes.First(x => x.Id == av.ClassId).DisplayName,
                        TypeLink = LinkEnum.IndirectLink,
                        Info = information,
                        UidForm = UidForm,
                        NameForm = NameForm
                    };
                }
                catch
                {
                    link = new Link
                    {
                        TypeLink = LinkEnum.ErrorIndirectLink,
                        Info = information,
                        UidForm = UidForm,
                        NameForm = NameForm
                    };
                }
            }
            else if (objItem is Discrete discrete)
            {
                try
                {
                    DiscreteValue dv = discrete.DiscreteValues.FirstOrDefault(x => x.MeasurementValueType.Uid == measurementValueType);
                    link = new Link
                    {
                        UidOI = dv.Uid,
                        NameOI = dv.name,
                        ClassOI = MImage.MetaData.Classes.First(x => x.Id == dv.ClassId).DisplayName,
                        TypeLink = LinkEnum.IndirectLink,
                        Info = information,
                        UidForm = UidForm,
                        NameForm = NameForm
                    };
                }
                catch
                {
                    link = new Link
                    {
                        TypeLink = LinkEnum.ErrorIndirectLink,
                        Info = information,
                        UidForm = UidForm,
                        NameForm = NameForm
                    };
                }
            }
            else if (elementGuid != Guid.Empty && measurementType != Guid.Empty && measurementValueType != Guid.Empty)
            {
                Guid measUid = Guid.Empty;
                IndirectLinkMeas indirectLink = new IndirectLinkMeas();
                indirectLink.PowerSystemResourceGuid = elementGuid;
                indirectLink.MeasTypeGuid = measurementType;
                indirectLink.ValueTypeGuid = measurementValueType;
                if (item.PhaseCode != null)
                {
                    indirectLink.SpecifiedPhaseCode = (MeasurementPhaseCode)item.PhaseCode;
                    indirectLink.SpecifiedPhaseStr = item.PhaseCode.ToString();
                }
                measUid = GetIndirectLink(indirectLink);
                if (measUid != Guid.Empty)
                {
                    MeasurementValue measVal = (MeasurementValue)MImage.GetObject(measUid);
                    var GVClass = MImage.MetaData.Classes.First(x => x.Id == measVal.ClassId);
                    link = new Link
                    {
                        UidOI = measVal.Uid,
                        NameOI = measVal.name,
                        ClassOI = GVClass.DisplayName,
                        TypeLink = LinkEnum.IndirectLink,
                        Info = information,
                        UidForm = UidForm,
                        NameForm = NameForm
                    };
                }
                else
                {
                    link = new Link
                    {
                        TypeLink = LinkEnum.ErrorIndirectLink,
                        Info = information,
                        UidForm = UidForm,
                        NameForm = NameForm
                    };
                }
            }
            return link;
        }
        private Link CheckedItem(SecondLevelBase item, string information)
        {
            Link link = new Link();
            Guid elementGuid = item.ElementGuid;
            SecondLevelBase idItem = (SecondLevelBase)item;
            Guid measurementType = idItem.MeasurementType;
            Guid measurementValueType = idItem.MeasurementValueType;
            var objItem = MImage.GetObject(elementGuid);
            if (objItem == null)
            {
                link = new Link
                {
                    TypeLink = LinkEnum.ErrorDirectLink,
                    Info = information,
                    UidForm = UidForm,
                    NameForm = NameForm
                };
            }
            else if (objItem is MeasurementValue measValue)
            {
                link = new Link
                {
                    UidOI = measValue.Uid,
                    NameOI = measValue.name,
                    ClassOI = MImage.MetaData.Classes.First(x => x.Id == measValue.ClassId).DisplayName,
                    TypeLink = LinkEnum.DirectLink,
                    Info = information,
                    UidForm = UidForm,
                    NameForm = NameForm
                };
            }
            else if (objItem is Analog analog)
            {
                try
                {
                    AnalogValue av = analog.AnalogValues.FirstOrDefault(x => x.MeasurementValueType.Uid == measurementValueType);
                    link = new Link
                    {
                        UidOI = av.Uid,
                        NameOI = av.name,
                        ClassOI = MImage.MetaData.Classes.First(x => x.Id == av.ClassId).DisplayName,
                        TypeLink = LinkEnum.IndirectLink,
                        Info = information,
                        UidForm = UidForm,
                        NameForm = NameForm
                    };
                }
                catch
                {
                    link = new Link
                    {
                        TypeLink = LinkEnum.ErrorIndirectLink,
                        Info = information,
                        UidForm = UidForm,
                        NameForm = NameForm
                    };
                }
            }
            else if (objItem is Discrete discrete)
            {
                try
                {
                    DiscreteValue dv = discrete.DiscreteValues.FirstOrDefault(x => x.MeasurementValueType.Uid == measurementValueType);
                    link = new Link
                    {
                        UidOI = dv.Uid,
                        NameOI = dv.name,
                        ClassOI = MImage.MetaData.Classes.First(x => x.Id == dv.ClassId).DisplayName,
                        TypeLink = LinkEnum.IndirectLink,
                        Info = information,
                        UidForm = UidForm,
                        NameForm = NameForm
                    };
                }
                catch
                {
                    link = new Link
                    {
                        TypeLink = LinkEnum.ErrorIndirectLink,
                        Info = information,
                        UidForm = UidForm,
                        NameForm = NameForm
                    };
                }
            }
            else if (elementGuid != Guid.Empty && measurementType != Guid.Empty && measurementValueType != Guid.Empty)
            {
                Guid measUid = Guid.Empty;
                IndirectLinkMeas indirectLink = new IndirectLinkMeas();
                indirectLink.PowerSystemResourceGuid = elementGuid;
                indirectLink.MeasTypeGuid = measurementType;
                indirectLink.ValueTypeGuid = measurementValueType;
                if (idItem.PhaseCode != null)
                {
                    indirectLink.SpecifiedPhaseCode = (MeasurementPhaseCode)idItem.PhaseCode;
                    indirectLink.SpecifiedPhaseStr = idItem.PhaseCode.ToString();
                }
                measUid = GetIndirectLink(indirectLink);
                if (measUid != Guid.Empty)
                {
                    MeasurementValue measVal = (MeasurementValue)MImage.GetObject(measUid);
                    var GVClass = MImage.MetaData.Classes.First(x => x.Id == measVal.ClassId);
                    link = new Link
                    {
                        UidOI = measVal.Uid,
                        NameOI = measVal.name,
                        ClassOI = GVClass.DisplayName,
                        TypeLink = LinkEnum.IndirectLink,
                        Info = information,
                        UidForm = UidForm,
                        NameForm = NameForm
                    };
                }
                else
                {
                    link = new Link
                    {
                        TypeLink = LinkEnum.ErrorIndirectLink,
                        Info = information,
                        UidForm = UidForm,
                        NameForm = NameForm
                    };
                }
            }


            return link;
        }
        public ObservableCollection<Link> CheckedDevIndicator(CircularIndicator indicator)
        {
            string information = "Объект индикатора [" + indicator.ZIndex + "]";
            ObservableRangeCollection<Link> LinkCollect = new ObservableRangeCollection<Link>();
            var obj = MImage.GetObject(indicator.ElementGuid);
            if (obj == null) { }
            else if (obj is MeasurementValue measVal)
            {
                LinkCollect.Add(new Link
                {
                    NameForm = NameForm,
                    UidForm = UidForm,
                    NameOI = measVal.name,
                    ClassOI = MImage.MetaData.Classes.First(x => x.Id == measVal.ClassId).DisplayName,
                    UidOI = measVal.Uid,
                    TypeLink = LinkEnum.DirectLink,
                    Info = information,
                });
            }
            foreach (var item in indicator.Items)
            {
                string Info = "Индикатор [" + indicator.ZIndex + "]\r\n" + item.ItemName;
                IndicatorItem idItem = (IndicatorItem)item;
                if (item.ElementGuid != Guid.Empty)
                    LinkCollect.Add(CheckedItem(idItem, Info));
            }
            return LinkCollect;
        }
        public ObservableCollection<Link> CheckedArrowIndicator(ArrowIndicator indicator)
        {
            string information = "Объект стрелки [" + indicator.ZIndex + "]";
            ObservableRangeCollection<Link> LinkCollect = new ObservableRangeCollection<Link>();
            var obj = MImage.GetObject(indicator.ElementGuid);
            if (obj == null) { }
            else if (obj is MeasurementValue measVal)
            {
                var objClass = MImage.MetaData.Classes.First(x => x.Id == measVal.ClassId);
                LinkCollect.Add(new Link
                {
                    NameForm = NameForm,
                    UidForm = UidForm,
                    NameOI = measVal.name,
                    ClassOI = objClass.DisplayName,
                    UidOI = measVal.Uid,
                    TypeLink = LinkEnum.DirectLink,
                    Info = information,
                });
            }
            foreach (var item in indicator.Items)
            {
                string info = "Стрелка [" + indicator.ZIndex + "]\r\n" + item.ItemName;
                IdentifiedIndicatorItem idItem = (IdentifiedIndicatorItem)item;
                if (item.ElementGuid != Guid.Empty)
                    LinkCollect.Add(CheckedItem(idItem, info));
            }
            return LinkCollect;
        }
        public ObservableCollection<Link> CheckedObjIndicator(Clock indicator)
        {
            string information = "Объект индикатора [" + indicator.ZIndex + "]";
            ObservableRangeCollection<Link> LinkCollect = new ObservableRangeCollection<Link>();
            var obj = MImage.GetObject(indicator.ElementGuid);
            if (obj == null) { }
            else if (obj is MeasurementValue measVal)
            {
                LinkCollect.Add(new Link
                {
                    NameForm = NameForm,
                    UidForm = UidForm,
                    NameOI = measVal.name,
                    ClassOI = MImage.MetaData.Classes.First(x => x.Id == measVal.ClassId).DisplayName,
                    UidOI = measVal.Uid,
                    TypeLink = LinkEnum.DirectLink,
                    Info = information,
                });
            }
            return LinkCollect;
        }
        public ObservableCollection<Link> CheckedObjButton(ButtonBase button)
        {
            var information = $"Объект кнопки [{ button.ZIndex}]";
            ObservableRangeCollection<Link> LinkCollect = new ObservableRangeCollection<Link>();
            Guid elementGuid = Guid.Empty;
            if (button is TelecontrolButton tb)
            {
                elementGuid = tb.ElementGuid;
            }
            if (button is HandControlButton hb)
            {
                elementGuid = hb.ElementGuid;
            }
            var obj = MImage.GetObject(elementGuid);
            if (obj == null) { }
            else if (obj is MeasurementValue measVal)
            {
                LinkCollect.Add(new Link
                {
                    NameForm = NameForm,
                    UidForm = UidForm,
                    NameOI = measVal.name,
                    ClassOI = MImage.MetaData.Classes.First(x => x.Id == measVal.ClassId).DisplayName,
                    UidOI = measVal.Uid,
                    TypeLink = LinkEnum.DirectLink,
                    Info = information,
                });
            }
            return LinkCollect;
        }

        public ObservableCollection<Link> CheckedObjIndicator(WeatherConditionIndicator indicator)
        {
            string information = "Объект индикатора [" + indicator.ZIndex + "]";
            ObservableRangeCollection<Link> LinkCollect = new ObservableRangeCollection<Link>();
            var obj = MImage.GetObject(indicator.ElementGuid);
            if (obj == null) { }
            else if (obj is MeasurementValue measVal)
            {
                LinkCollect.Add(new Link
                {
                    NameForm = NameForm,
                    UidForm = UidForm,
                    NameOI = measVal.name,
                    ClassOI = MImage.MetaData.Classes.First(x => x.Id == measVal.ClassId).DisplayName,
                    UidOI = measVal.Uid,
                    TypeLink = LinkEnum.DirectLink,
                    Info = information
                });
            }
            return LinkCollect;
        }
        public ObservableCollection<Link> CheckedObjIndicator(ZVKInCell indicator)
        {
            string information = $"Объект индикатора [{indicator.ZIndex}]";
            ObservableRangeCollection<Link> LinkCollect = new ObservableRangeCollection<Link>();
            var obj = MImage.GetObject(indicator.ElementGuid);
            if (obj == null) { }
            else if (obj is MeasurementValue measVal)
            {
                LinkCollect.Add(new Link
                {
                    NameForm = NameForm,
                    UidForm = UidForm,
                    NameOI = measVal.name,
                    ClassOI = MImage.MetaData.Classes.First(x => x.Id == measVal.ClassId).DisplayName,
                    UidOI = measVal.Uid,
                    TypeLink = LinkEnum.DirectLink,
                    Info = information
                });
            }
            return LinkCollect;
        }

        /// <summary>
        /// Проверка формул
        /// </summary>
        /// <param name="expColold"></param>
        /// <returns></returns>
        public ObservableCollection<Link> CheckExpressions(XElement expColold, string information)
        {
            ObservableCollection<Link> LinkCollect = new ObservableCollection<Link>();
            try
            {               
                var exprList = expColold.Element("CustomData")
                                                    .Element("Item")
                                                    .Element("ExpressionCollection")
                                                    .Elements("LExpression");
                foreach (var exp in exprList)
                {
                    try
                    {
                        var allOperandsOld = exp.Element("Expression").Element("Operands").Elements("Operand");
                        foreach (var operand in allOperandsOld)
                        {
                            string info = $"Формула: { exp.Attribute("name")} Операнд: {operand.Attribute("Name")} {information}";
                            if (operand.Attribute("Type").Value == "MeasValueOperand")
                            {
                                var measGuid = new Guid(operand.Element("MeasurementValue").Value);
                                try
                                {
                                    MeasurementValue measValue = (MeasurementValue)MImage.GetObject(measGuid);
                                    LinkCollect.Add(new Link
                                    {
                                        UidOI = measValue.Uid,
                                        NameOI = measValue.name,
                                        ClassOI = MImage.MetaData.Classes.First(x => x.Id == measValue.ClassId).DisplayName,
                                        TypeLink = LinkEnum.DirectLink,
                                        Info = info,
                                        UidForm = UidForm,
                                        NameForm = NameForm
                                    });
                                }
                                catch
                                {
                                    LinkCollect.Add(new Link
                                    {
                                        TypeLink = LinkEnum.ErrorDirectLink,
                                        Info = info,
                                        UidForm = UidForm,
                                        NameForm = NameForm
                                    });
                                }
                            }
                            else if (operand.Attribute("Type").Value == "PSRMeasOperand")
                            {
                                var indirectLink = new IndirectLinkMeas();
                                indirectLink.PowerSystemResourceGuid = new Guid(operand.Element("PowerSystemResource").Value);
                                indirectLink.MeasTypeGuid = new Guid(operand.Element("MeasurementType").Value);
                                indirectLink.ValueTypeGuid = new Guid(operand.Element("ValueType").Value);
                                try
                                {
                                    indirectLink.SpecifiedTerminal = new Guid(operand.Element("SpecifiedTerminal").Value);
                                }
                                catch (NullReferenceException) { }
                                try
                                {
                                    indirectLink.SpecifiedPhaseStr = operand.Element("SpecifiedPhaseCode").Value.ToString();
                                }
                                catch (NullReferenceException) { }
                                var measGuid = GetIndirectLink(indirectLink);
                                if (measGuid == Guid.Empty)
                                {
                                    LinkCollect.Add(new Link
                                    {
                                        TypeLink = LinkEnum.ErrorIndirectLink,
                                        Info = info,
                                        UidForm = UidForm,
                                        NameForm = NameForm
                                    });
                                }
                                else
                                {
                                    MeasurementValue measValue = (MeasurementValue)MImage.GetObject(measGuid);
                                    LinkCollect.Add(new Link
                                    {
                                        UidOI = measValue.Uid,
                                        NameOI = measValue.name,
                                        ClassOI = MImage.MetaData.Classes.First(x => x.Id == measValue.ClassId).DisplayName,
                                        TypeLink = LinkEnum.IndirectLink,
                                        Info = info,
                                        UidForm = UidForm,
                                        NameForm = NameForm
                                    });
                                }
                            }
                        }
                    }
                    catch
                    {
                        LinkCollect.Add(new Link
                        {
                            TypeLink = LinkEnum.ErrorDirectLink,
                            Info = "Формула: " + exp.Attribute("name") + information,
                            UidForm = UidForm,
                            NameForm = NameForm
                        });
                    }
                }
            }
            catch (NullReferenceException) { }

            return LinkCollect;
        }
        /// <summary>
        /// Получение измерения по косвенной ссылке
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        private Guid GetIndirectLink(IndirectLinkMeas link)
        {
            try
            {
                var obj = MImage.GetObject(link.PowerSystemResourceGuid);
                Terminal tm = obj as Terminal;
                if (tm != null)
                {
                    link.PowerSystemResourceGuid = tm.ConductingEquipment.Uid;
                    obj = MImage.GetObject(link.PowerSystemResourceGuid);
                    link.SpecifiedTerminal = tm.Uid;
                }
                ProtectionEquipmentContainer pEC = obj as ProtectionEquipmentContainer;
                if (pEC != null)
                {
                    return Guid.Empty;
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
                else if (meas is Analog)
                {
                    Analog a = (Analog)meas;
                    var av = a.AnalogValues.FirstOrDefault(x => x.MeasurementValueType.Uid == link.ValueTypeGuid);
                    if (av != null)
                        return av.Uid;
                    else
                        return Guid.Empty;
                }
                else if (meas is Discrete)
                {
                    Discrete d = (Discrete)meas;
                    var dv = d.DiscreteValues.FirstOrDefault(x => x.MeasurementValueType.Uid == link.ValueTypeGuid);
                    if (dv != null)
                        return dv.Uid;
                    else
                        return Guid.Empty;
                }
            }
            catch (InvalidCastException) { }
            return Guid.Empty;
        }
    }

}
