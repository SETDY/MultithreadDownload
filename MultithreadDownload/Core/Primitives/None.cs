using System;
using System.Collections.Generic;
using System.Text;

namespace MultithreadDownload.Core.Primitives
{
    /// <summary>
    /// A class is used for representing a None value
    /// </summary>
    public class None
    {
        public None(params object[] parameters) { }

        public static None Create(params object[] parameters) 
        { 
            return new None(parameters); 
        }
    }
}
