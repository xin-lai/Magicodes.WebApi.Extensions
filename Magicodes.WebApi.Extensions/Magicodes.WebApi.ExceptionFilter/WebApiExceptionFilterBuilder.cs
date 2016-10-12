using Magicodes.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Magicodes.WebApi.ExceptionFilter
{
    public class WebApiExceptionFilterBuilder
    {
        protected LoggerBase Logger { get; set; }
        /// <summary>
        ///     创建实例
        /// </summary>
        /// <returns></returns>
        public static WebApiExceptionFilterBuilder Create()
        {
            return new WebApiExceptionFilterBuilder();
        }
        /// <summary>
        ///    设置默认处理函数
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public WebApiExceptionFilterBuilder WithDefaultHandler(Func<Exception, HttpRequestMessage, LoggerBase, HttpResponseMessage> handler)
        {
            WebApiExceptionFilter.DefaultHandler = handler;
            return this;
        }

        /// <summary>
        ///     添加日志记录器
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        public WebApiExceptionFilterBuilder WithLogger(LoggerBase logger)
        {
            Logger = logger;
            return this;
        }


        //Register<KeyNotFoundException>(HttpStatusCode.NotFound)
        //Register<SecurityException>(HttpStatusCode.Forbidden)
        public WebApiExceptionFilterBuilder Register<TException>(HttpStatusCode statusCode)
            where TException : Exception
        {
            var type = typeof(TException);
            var item = new Tuple<HttpStatusCode?, Func<Exception, HttpRequestMessage, LoggerBase, HttpResponseMessage>>(
                statusCode, WebApiExceptionFilter.DefaultHandler
            );

            if (!WebApiExceptionFilter.Handlers.TryAdd(type, item))
            {
                Tuple<HttpStatusCode?, Func<Exception, HttpRequestMessage, LoggerBase, HttpResponseMessage>> oldItem = null;
                if (WebApiExceptionFilter.Handlers.TryRemove(type, out oldItem))
                {
                    WebApiExceptionFilter.Handlers.TryAdd(type, item);
                }
            }
            return this;
        }

        //    Register<SqlException>(
        //      (exception, request) =>
        //      {
        //          var sqlException = exception as SqlException;

        //          if (sqlException.Number > 50000)
        //          {
        //              var response = request.CreateResponse(HttpStatusCode.BadRequest);
        //      response.ReasonPhrase   = sqlException.Message.Replace(Environment.NewLine, String.Empty);

        //              return response;
        //          }
        //          else
        //          {
        //              return request.CreateResponse(HttpStatusCode.InternalServerError);
        //          }
        //      }
        //    )
        public WebApiExceptionFilterBuilder Register<TException>(Func<Exception, HttpRequestMessage, LoggerBase, HttpResponseMessage> handler)
            where TException : Exception
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            var type = typeof(TException);
            var item = new Tuple<HttpStatusCode?, Func<Exception, HttpRequestMessage, LoggerBase, HttpResponseMessage>>(
                null, handler
            );
            if (!WebApiExceptionFilter.Handlers.TryAdd(type, item))
            {
                Tuple<HttpStatusCode?, Func<Exception, HttpRequestMessage, LoggerBase, HttpResponseMessage>> oldItem = null;
                if (WebApiExceptionFilter.Handlers.TryRemove(type, out oldItem))
                {
                    WebApiExceptionFilter.Handlers.TryAdd(type, item);
                }
            }
            return this;
        }

        public WebApiExceptionFilterBuilder Unregister<TException>()
            where TException : Exception
        {
            Tuple<HttpStatusCode?, Func<Exception, HttpRequestMessage, LoggerBase, HttpResponseMessage>> item = null;
            WebApiExceptionFilter.Handlers.TryRemove(typeof(TException), out item);
            return this;
        }

        /// <summary>
        ///     执行
        /// </summary>
        public void Build()
        {
            WebApiExceptionFilter.Logger = Logger ?? new NullLogger("WebApiExceptionFilter");
        }
    }
}
