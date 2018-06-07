using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace WinService.Debugger
{
    public interface IDebuggableService
    {
        void Start(string[] args);
        void Stop();
        void Pause();
        void Continue();
        EventLog GetEventLog();
    }
}
