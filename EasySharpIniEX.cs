using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TABHelperMod
{
    public static class EasySharpIniEX
    {
        public static EasySharpIni.Models.IniField GetField(this EasySharpIni.IniFile iniFile, GamePatch.FuncConfigList funcConfigList, string defalutValue = "")
        {
            return iniFile.GetField(funcConfigList.ToString(), defalutValue);
        }
    }
}
