using Monitel.Diogen.Elements.Tables;
using Monitel.Diogen.Infrastructure;
using Monitel.Mal;
using Monitel.Mal.Context.CIM16;
using OperInformApp.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OperInformApp.ViewModel.AppViewModelBase;

namespace OperInformApp.Foundation
{
	class CorrectLink
	{
		public ModelImage MImage { get; set; }
		public Guid UidForm { get; set; }
		public string NameForm { get; set; }
		/*public ObservableCollection<Link> CorrectTimeCell(TimeCell timeCell, MeasurementValue mv)
		{
			string information = $"Ячейка ({timeCell.Row};{timeCell.Column}) [{timeCell.ZIndex}]";
			if (timeCell.IsLinked)
			{	
				try
				{
					var link = GetIndirectLink(mv);
					MImage.BeginTransaction();
					timeCell.ElementGuid = link.elementGuid;
					timeCell.MeasurementType = link.measurementType;
					timeCell.MeasurementValueType = link.measurementValueType;
					if (link.measurementSource != Guid.Empty)
						timeCell.MeasurementValueSource = link.measurementSource;
					if (link.phase != null)
						timeCell.PhaseCode = (MeasurementPhaseCode)link.phase;
					MImage.CommitTransaction();
				}
				catch (Exception ex)
				{
					MImage.RollbackTransaction();

				}
				
			}
			else if (timeCell.HasLocalExpressions)
			{
				XElement expColold = new XElement("ExpressionCollection");
				timeCell.CustomData.XLWrite(expColold);
				LinkCollect.AddRange(CheckExpressions(expColold, information));
			}
			return LinkCollect;
		}*/
		private static (Guid elementGuid, Guid measurementType, Guid measurementValueType, Guid measurementSource, PhaseCode? phase) GetIndirectLink(MeasurementValue mv)
		{
			Guid elementGuid;
			Guid measurementType;
			Guid measurementSource = Guid.Empty;
			Guid measurementValueType = mv.MeasurementValueType.Uid;
			PhaseCode? phase;
			if (mv is AnalogValue av)
			{
				if (av.Analog.PowerSystemResource == null)
				{
					elementGuid = av.Analog.Uid;
				}
				else
				{
					if (av.Analog.Terminal == null)
						elementGuid = av.Analog.PowerSystemResource.Uid;
					else
						elementGuid = av.Analog.Terminal.Uid;
				}
				if (av.Analog.AnalogValues.Where(x => x.MeasurementValueType == av.MeasurementValueType).Count() > 1)
				{
					measurementSource = av.MeasurementValueSource.Uid;
				}
				measurementType = av.Analog.MeasurementType.Uid;
				phase = av.Analog.phases;
			}
			else
			{
				DiscreteValue dv = mv as DiscreteValue;
				if (dv.Discrete.PowerSystemResource == null)
				{
					elementGuid = dv.Discrete.Uid;
				}
				else
				{
					if (dv.Discrete.Terminal == null)
						elementGuid = dv.Discrete.PowerSystemResource.Uid;
					else
						elementGuid = dv.Discrete.Terminal.Uid;
				}
				if (dv.Discrete.DiscreteValues.Where(x => x.MeasurementValueType == dv.MeasurementValueType).Count() > 1)
				{
					measurementSource = dv.MeasurementValueSource.Uid;
				}
				measurementType = dv.Discrete.MeasurementType.Uid;
				phase = dv.Discrete.phases;
			}
			return (elementGuid, measurementType, measurementValueType, measurementSource, phase);

		}
	}
}
