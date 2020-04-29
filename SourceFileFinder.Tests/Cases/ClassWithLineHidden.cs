using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReflectionHelpers.Cases
{
    public class ClassWithLineHidden
    {
#line hidden
        void Foo()
        {

        }
    }
}
