using Hangfire;
using Hangfire.HttpJob.Server;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace JobsServer.Controllers
{
    /// <summary>
    /// 通过接口形式去添加定时任务的方法
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AddJobsController : Controller
    {
        /// <summary>
        /// 添加一个队列任务立即被执行
        /// </summary>
        /// <param name="httpJob"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult AddBackGroundJob([FromBody] HttpJobItem httpJob)
        {
            var addreslut = string.Empty;
            try
            {
                addreslut = BackgroundJob.Enqueue(() => HttpJob.Excute(httpJob, httpJob.JobName, httpJob.QueueName, httpJob.IsRetry, null));
            }
            catch (Exception ec)
            {
                return Json(new Message() { Code = false, ErrorMessage = ec.ToString() });
            }
            return Json(new Message() { Code = true, ErrorMessage = "" });
        }

        /// <summary>
        /// 添加一个周期任务
        /// </summary>
        /// <param name="httpJob"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult AddOrUpdateRecurringJob(HttpJobItem httpJob)
        {
            try
            {
                RecurringJob.AddOrUpdate(httpJob.JobName, () => HttpJob.Excute(httpJob, httpJob.JobName, httpJob.QueueName, httpJob.IsRetry, null), httpJob.Corn, TimeZoneInfo.Local);
            }
            catch (Exception ec)
            {
                return Json(new Message() { Code = false, ErrorMessage = ec.ToString() });
            }
            return Json(new Message() { Code = true, ErrorMessage = "" });
        }

        /// <summary>
        /// 删除一个周期任务
        /// </summary>
        /// <param name="jobname"></param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult DeleteJob(string jobname)
        {
            try
            {
                RecurringJob.RemoveIfExists(jobname);
            }
            catch (Exception ec)
            {
                return Json(new Message() { Code = false, ErrorMessage = ec.ToString() });
            }
            return Json(new Message() { Code = true, ErrorMessage = "" });
        }

        /// <summary>
        /// 手动触发一个任务
        /// </summary>
        /// <param name="jobname"></param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult TriggerRecurringJob(string jobname)
        {
            try
            {
                RecurringJob.Trigger(jobname);
            }
            catch (Exception ec)
            {
                return Json(new Message() { Code = false, ErrorMessage = ec.ToString() });
            }
            return Json(new Message() { Code = true, ErrorMessage = "" });
        }

        /// <summary>
        /// 添加一个延迟任务
        /// </summary>
        /// <param name="httpJob">httpJob.DelayFromMinutes（延迟多少分钟执行）</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult AddScheduleJob([FromBody] HttpJobItem httpJob)
        {
            var reslut = string.Empty;
            try
            {
                reslut = BackgroundJob.Schedule(() => HttpJob.Excute(httpJob, httpJob.JobName, httpJob.QueueName, httpJob.IsRetry, null), TimeSpan.FromMinutes(httpJob.DelayFromMinutes));
            }
            catch (Exception ec)
            {
                return Json(new Message() { Code = false, ErrorMessage = ec.ToString() });
            }
            return Json(new Message() { Code = true, ErrorMessage = "" });
        }

        /// <summary>
        /// 添加连续任务,多个任务依次执行，只执行一次
        /// </summary>
        /// <param name="httpJob"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult AddContinueJob([FromBody] List<HttpJobItem> httpJobItems)
        {
            var reslut = string.Empty;
            var jobid = string.Empty;
            try
            {
                httpJobItems.ForEach(k =>
                {
                    if (!string.IsNullOrEmpty(jobid))
                    {
                        jobid = BackgroundJob.ContinueJobWith(jobid, () => RunContinueJob(k));
                    }
                    else
                    {
                        jobid = BackgroundJob.Enqueue(() => HttpJob.Excute(k, k.JobName, k.QueueName, k.IsRetry, null));
                    }
                });
                reslut = "true";
            }
            catch (Exception ec)
            {
                return Ok(new Message() { Code = false, ErrorMessage = ec.ToString() });
            }
            return Ok(new Message() { Code = true, ErrorMessage = "" });
        }

        /// <summary>
        /// 执行连续任务
        /// </summary>
        /// <param name="httpJob"></param>
        public void RunContinueJob(HttpJobItem httpJob)
        {
            BackgroundJob.Enqueue(() => HttpJob.Excute(httpJob, httpJob.JobName, httpJob.QueueName, httpJob.IsRetry, null));
        }
    }

    /// <summary>
    /// 返回消息
    /// </summary>
    public class Message
    {
        public bool Code { get; set; }
        public string ErrorMessage { get; set; }
    }
}