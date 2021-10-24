using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace AssemblyBrowserLib
{
    public class ExtensionMethodInfo
    {
        public ExtensionMethodInfo(MethodInfo methodInfo, string methodSignature)
        {
            MethodSignature = methodSignature;
            MethodInfo = methodInfo;
        }

        public MethodInfo MethodInfo { get; set; }
        public string MethodSignature { get; set; }
    }
}