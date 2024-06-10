using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProbeSampler.Core.Enums
{
    public enum AuthorizationStatus
    {
        Success,
        WaitForPassword,
        WrongPassword,
        Closed,
    }
}
