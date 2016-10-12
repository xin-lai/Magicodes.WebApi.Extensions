using Magicodes.Logger;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Filters;

namespace Magicodes.WebApi.ExceptionFilter
{
    public class WebApiExceptionFilter : ExceptionFilterAttribute
    {
        internal static LoggerBase Logger { get; set; }

        #region DefaultHandler
        /// <summary>
        /// 默认的异常处理函数
        /// </summary>
        internal static Func<Exception, HttpRequestMessage, LoggerBase, HttpResponseMessage> DefaultHandler = (exception, request, logger) =>
         {
             if (exception == null)
             {
                 return null;
             }

             var response = request.CreateResponse<string>(
                 HttpStatusCode.InternalServerError, GetContentOf(exception)
             );
             response.ReasonPhrase = exception.Message.Replace(Environment.NewLine, String.Empty);

             return response;
         };

        #endregion

        #region GetContentOf
        /// <summary>
        /// 获取异常详细信息
        /// </summary>
        /// <value>
        ///  <see cref="Func{Exception, String}"/> 该函数返回异常详细
        /// </value>
        public static Func<Exception, string> GetContentOf = (exception) =>
        {
            if (exception == null)
                return String.Empty;

            var result = new StringBuilder();

            result.AppendLine(exception.Message);
            result.AppendLine();

            Exception innerException = exception.InnerException;
            while (innerException != null)
            {
                result.AppendLine(innerException.Message);
                result.AppendLine();
                innerException = innerException.InnerException;
            }

#if DEBUG
            result.AppendLine(exception.StackTrace);
#endif
            return result.ToString();
        };
        #endregion

        #region Handlers
        /// <summary>
        /// 异常处理函数
        /// </value>
        internal static ConcurrentDictionary<Type, Tuple<HttpStatusCode?, Func<Exception, HttpRequestMessage, LoggerBase, HttpResponseMessage>>> Handlers
        {
            get
            {
                return _filterHandlers;
            }
        }
        private static readonly ConcurrentDictionary<Type, Tuple<HttpStatusCode?, Func<Exception, HttpRequestMessage, LoggerBase, HttpResponseMessage>>> _filterHandlers = new ConcurrentDictionary<Type, Tuple<HttpStatusCode?, Func<Exception, HttpRequestMessage, LoggerBase, HttpResponseMessage>>>();
        #endregion



        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext == null || actionExecutedContext.Exception == null)
                return;
            //记录日志
            Logger.LogFormat(LoggerLevels.Error, "Url:{0};{1}Detail:{2}", actionExecutedContext.Request.RequestUri.AbsoluteUri, Environment.NewLine, GetContentOf(actionExecutedContext.Exception));
            var type = actionExecutedContext.Exception.GetType();
            Tuple<HttpStatusCode?, Func<Exception, HttpRequestMessage, LoggerBase, HttpResponseMessage>> registration = null;

            if (Handlers.TryGetValue(type, out registration))
            {
                var statusCode = registration.Item1;
                var handler = registration.Item2;
                //处理异常
                var response = handler(
                    actionExecutedContext.Exception.GetBaseException(),
                    actionExecutedContext.Request,
                    Logger
                );
                if (statusCode.HasValue)
                    response.StatusCode = statusCode.Value;

                actionExecutedContext.Response = response;
            }
            else
            {
                //如果没有注册相关异常处理，则使用默认的异常处理
                actionExecutedContext.Response = DefaultHandler(
                    actionExecutedContext.Exception.GetBaseException(), actionExecutedContext.Request, Logger
                );
            }
        }
    }
}
