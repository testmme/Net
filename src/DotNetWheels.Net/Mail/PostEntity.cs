using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace DotNetWheels.Net.Mail
{
    /// <summary>
    /// 投递实体
    /// </summary>
    [Serializable]
    internal class PostEntity : IEquatable<PostEntity>
    {
        private SmtpClient _smtpClient;
        private MailMessage _message;
        private Boolean _prepareToDispose;
        private const String EmptyKey = "F3587402-B44E-46D8-9860-4ED08F64E5CA";

        public Boolean PrepareToDispose
        {
            get { return _prepareToDispose; }
        }

        private PostEntity(SmtpClient client)
        {
            _prepareToDispose = true;
            _smtpClient = client;
        }

        public PostEntity(SmtpClient client, MailMessage message)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            _smtpClient = client;
            _message = message;

            _prepareToDispose = false;
        }

        /// <summary>
        /// 获取一个标识可以被释放的PostEntity对象。
        /// 将该实例添加到邮件队列之后，在处理该邮件时，系统将不再发送邮件，而是会调用该SmtpClient的Dispose方法以释放资源。
        /// </summary>
        public static PostEntity CreateDisposedEntity(SmtpClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            return new PostEntity(client);
        }

        public String Key
        {
            get
            {
                if (_message != null)
                {
                    return _message.Body;
                }

                return EmptyKey;
            }
        }

        internal MailMessage Message
        {
            get { return _message; }
        }

        public SmtpClient SmtpClient
        {
            get { return _smtpClient; }
        }

        public Int32 GetHashCode(PostEntity entity)
        {
            return entity.Key.GetHashCode();
        }

        public Boolean Equals(PostEntity other)
        {
            return other != null && this.Key == other.Key;
        }
    }
}
