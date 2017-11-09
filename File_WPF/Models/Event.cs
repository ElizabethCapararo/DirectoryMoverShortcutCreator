using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class Event
    {
        public enum ErrorTypes { Info, Warning, Error }

        public string Error { get; set; }

        public void SetError(ErrorTypes errorType, string error)
        {
            if (errorType == ErrorTypes.Info)
                Error = "INFO: ";
            else if (errorType == ErrorTypes.Warning)
                Error = "WARNING: ";
            else if (errorType == ErrorTypes.Error)
                Error = "ERROR: ";

            Error += error + ": " + DateTime.Now;
        }

        public Event(ErrorTypes eT, string e) { SetError(eT, e);}

        public Event() { Error = null; }
    }
}
