using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CitizenSerialInfo.Services
{
    public interface IEmailSender
    {
        String SendEmail(string email, string subject, string message);
    }
}
