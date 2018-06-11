using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using DotNetWheels.Security;

namespace DotNetWheels.Net.Mail
{
    internal class PostMessage : MailMessage
    {

        public String Key
        {
            get
            {
                return CryptoHelper.GetMD5(this.ToString());
            }
        }

        public override Boolean Equals(Object obj)
        {
            if (obj == null) { return false; }
            if (this.GetType() != obj.GetType()) { return false; }
            var oMsg = obj as PostMessage;
            if (Key != oMsg.Key) { return false; }
            return true;
        }

        public override Int32 GetHashCode()
        {
            return this.Key.GetHashCode();
        }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.From.Address);
            foreach (var to in this.To) {
                sb.Append(to.Address);
            }
            foreach (var bcc in this.Bcc) {
                sb.Append(bcc.Address);
            }
            foreach (var cc in this.CC) {
                sb.Append(cc.Address);
            }
            foreach (var attachment in this.Attachments) {
                sb.Append(attachment.Name);
            }
            sb.Append(CryptoHelper.GetMD5(this.Body));
            return sb.ToString();
        }
    }
}
