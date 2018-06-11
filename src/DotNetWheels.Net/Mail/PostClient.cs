using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetWheels.Core.Threading;

namespace DotNetWheels.Net.Mail
{
    /// <summary>
    /// 邮寄客户端。
    /// </summary>
    public sealed class PostClient : IDisposable
    {
        private const Int32 BatchSendSize = 10;
        private const Int32 Timeout = 5;

        private SmtpClient _client;
        private MailAddress _mainAccount;
        private MailAddress[] _receiveAccounts;
        private MailAddress[] _bccAccounts;
        private MailAddress[] _ccAccounts;

        private static ConcurrentQueue<PostEntity> _messages;
        private static TimeoutTimer _timer;

        static PostClient()
        {
            _messages = new ConcurrentQueue<PostEntity>();
            //默认5秒轮询一次
            _timer = new TimeoutTimer(Timeout);
            _timer.Tick += Timer_TimeoutEvent;
            _timer.Start();
        }


        /// <summary>
        /// 初始化邮寄客户端。
        /// </summary>
        /// <param name="host">SMTP服务器地址</param>
        /// <param name="port">SMTP服务器端口</param>
        /// <param name="useSSL">是否使用SSL连接</param>
        public PostClient(String host, Int32 port = 25, Boolean useSSL = false)
        {
            if (String.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentNullException("host");
            }

            _client = new SmtpClient(host, port > 0 ? port : 25)
            {
                EnableSsl = useSSL
            };
        }

        /// <summary>
        /// 绑定主账号
        /// </summary>
        /// <param name="mainAccount">主账号邮件地址</param>
        public PostClient BindMainAccount(MailAddress mainAccount)
        {
            if (mainAccount == null)
            {
                throw new ArgumentNullException("account");
            }

            _mainAccount = mainAccount;

            return this;
        }

        /// <summary>
        /// 绑定主账号使用的凭证
        /// </summary>
        /// <param name="credential">网络凭证，通常由用户名和密码组成。</param>
        public PostClient BindCredential(NetworkCredential credential)
        {
            if (credential == null)
            {
                throw new ArgumentNullException("credential");
            }

            _client.Credentials = credential;

            return this;
        }

        /// <summary>
        /// 绑定收件地址
        /// </summary>
        /// <param name="receiveAccount">收件地址</param>
        /// <param name="bccAccounts">设置抄送地址</param>
        /// <param name="ccAccounts">设置密送地址</param>
        public PostClient BindReceiveAccount(MailAddress receiveAccount, MailAddress[] bccAccounts = null, MailAddress[] ccAccounts = null)
        {
            return BindReceiveAccounts(new MailAddress[] { receiveAccount }, bccAccounts, ccAccounts);
        }

        /// <summary>
        /// 绑定收件地址集合
        /// </summary>
        /// <param name="receiveAccounts">收件地址集合</param>
        /// <param name="bccAccounts">设置抄送地址</param>
        /// <param name="ccAccounts">设置密送地址</param>
        public PostClient BindReceiveAccounts(MailAddress[] receiveAccounts, MailAddress[] bccAccounts = null, MailAddress[] ccAccounts = null)
        {
            if (receiveAccounts == null)
            {
                throw new ArgumentNullException("receiveAddresses");
            }

            _receiveAccounts = receiveAccounts;
            _bccAccounts = bccAccounts;
            _ccAccounts = ccAccounts;

            return this;
        }

        /// <summary>
        /// 执行发送邮件操作。
        /// </summary>
        /// <param name="mailBody">邮件正文</param>
        /// <param name="mainTitle">邮件标题</param>
        /// <param name="bodyIsHtml">设置正文是否为HTML格式内容</param>
        /// <param name="attachments">设置附件</param>
        public void SendMail(String mailBody, String mainTitle, Boolean bodyIsHtml = false, Attachment[] attachments = null)
        {

            MailMessage msg = new MailMessage()
            {
                From = this._mainAccount,
                Body = mailBody,
                BodyEncoding = Encoding.UTF8,
                Subject = mainTitle,
                SubjectEncoding = Encoding.UTF8,
                IsBodyHtml = bodyIsHtml
            };

            #region #设置抄送

            if (attachments != null && attachments.Length > 0)
            {
                foreach (var atm in attachments)
                {
                    msg.Attachments.Add(atm);
                }
            }

            if (this._receiveAccounts != null && this._receiveAccounts.Length > 0)
            {
                foreach (var ac in this._receiveAccounts)
                {
                    msg.To.Add(ac);
                }
            }

            if (this._bccAccounts != null && this._bccAccounts.Length > 0)
            {
                foreach (var bcc in this._bccAccounts)
                {
                    msg.Bcc.Add(bcc);
                }
            }

            if (this._ccAccounts != null && this._ccAccounts.Length > 0)
            {
                foreach (var cc in this._ccAccounts)
                {
                    msg.CC.Add(cc);
                }
            }

            #endregion

            var entity = new PostEntity(this._client, msg);

            //如果已经存在相同的邮件，则不添加到邮件队列
            if (_messages.Count(x => x.Key == entity.Key) > 0)
            {
                return;
            }

            //否则将邮件添加到队列
            _messages.Enqueue(entity);
        }

        public void Dispose()
        {
            //释放操作是将一个释放对象放入发送队列，然后当处理到该对象时进行真正的释放操作
            var dispEntity = PostEntity.CreateDisposedEntity(this._client);
            _messages.Enqueue(dispEntity);
        }

        static void Timer_TimeoutEvent(Object sender, EventArgs e)
        {
            for (var i = 0; i < BatchSendSize; i++)
            {
                PostEntity entity = null;

                if (_messages.TryDequeue(out entity))
                {
                    if (entity.PrepareToDispose)
                    {
                        entity.SmtpClient.Dispose();
                        return;
                    }

                    try
                    {
                        entity.SmtpClient.Send(entity.Message);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
        }
    }
}
