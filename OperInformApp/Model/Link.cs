using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monitel.Diogen.Infrastructure;

namespace OperInformApp.Model
{
    class Link
    {
        /// <summary>
        /// Название формы\формулы
        /// </summary>
        public string NameForm { get; set; }
        /// <summary>
        /// UID формы\формулы
        /// </summary>
        public Guid UidForm { get; set; }
        /// <summary>
        /// Класс ОИ
        /// </summary>
        public string ClassOI { get; set; }
        /// <summary>
        /// Название ОИ в ИМ
        /// </summary>
        public string NameOI { get; set; }
        /// <summary>
        /// UID ОИ
        /// </summary>
        public Guid UidOI { get; set; }
        /// <summary>
        /// Тип ссылки (прямая/косвенная)
        /// </summary>
        public string TypeLinkStr 
        { 
            get 
            {
                if (TypeLink == LinkEnum.DirectLink)
                    return "Прямая ссылка";
                else if (TypeLink == LinkEnum.IndirectLink)
                    return "Косвенная ссылка";
                if (TypeLink == LinkEnum.ErrorIndirectLink)
                    return "Ошибка косвенная ссылка";
                else 
                    return "Ошибка прямая ссылка";
            } 
        }
        public LinkEnum TypeLink { get; set; }
        /// <summary>
        /// Описание места привязки ОИ
        /// </summary>
        public string Info { get; set; }
        public int IndexElement { get; set; }

        public enum LinkEnum
        {
            DirectLink,
            IndirectLink,
            ErrorDirectLink,
            ErrorIndirectLink
        }


    }
    public class IndirectLinkMeas
    {
        public Guid PowerSystemResourceGuid;
        public Guid MeasTypeGuid;
        public Guid ValueTypeGuid;
        public Guid SpecifiedTerminal;
        public MeasurementPhaseCode SpecifiedPhaseCode;
        public string SpecifiedPhaseStr;
        public string Field;

    }
}
