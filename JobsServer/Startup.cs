using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard.BasicAuthorization;
using Hangfire.Heartbeat;
using Hangfire.HttpJob;
using Hangfire.HttpJob.Support;
using Hangfire.MySql;
using Hangfire.Server;
using JobsServer.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace JobsServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public static readonly string[] ApiQueues = new[] { "apis", "jobs", "task", "rjob", "pjob", "rejob", "default" };
        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();

            services.AddHangfire(
                config =>
                {
                    //使用服务器资源监视
                    config.UseHeartbeatPage(checkInterval: TimeSpan.FromSeconds(1));
                    if (HangfireSettings.Instance.UseMySql)
                    {
                        //使用mysql配置
                        config.UseStorage(new MySqlStorage(HangfireSettings.Instance.HangfireMysqlConnectionString,
                            new MySqlStorageOptions
                            {
                                TablesPrefix = "hangfire",
                                TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted,// IsolationLevel.ReadCommitted,//实物隔离级别，默认为读取已提交
                                QueuePollInterval = TimeSpan.FromSeconds(1),//队列检测频率，秒级任务需要配置短点，一般任务可以配置默认时间
                                JobExpirationCheckInterval = TimeSpan.FromHours(1),//作业到期检查间隔（管理过期记录）。默认值为1小时
                                CountersAggregateInterval = TimeSpan.FromMinutes(5),//聚合计数器的间隔。默认为5分钟
                                PrepareSchemaIfNecessary = true,//设置true，则会自动创建表
                                DashboardJobListLimit = 50000,//仪表盘作业列表展示条数限制
                                TransactionTimeout = TimeSpan.FromMinutes(1),//事务超时时间，默认一分钟
                            })).UseConsole(new ConsoleOptions()
                            {
                                BackgroundColor = "#000079"
                            })//使用日志展示
                            .UseHangfireHttpJob(new HangfireHttpJobOptions()
                            {
                                SendToMailList = HangfireSettings.Instance.SendMailList,
                                SendMailAddress = HangfireSettings.Instance.SendMailAddress,
                                SMTPServerAddress = HangfireSettings.Instance.SMTPServerAddress,
                                SMTPPort = HangfireSettings.Instance.SMTPPort,
                                SMTPPwd = HangfireSettings.Instance.SMTPPwd,
                                SMTPSubject = HangfireSettings.Instance.SMTPSubject
                            });//启用http任务
                    }
                }
                );


            services.AddSignalR();

            //跨域设置
            services.AddCors(options => options.AddPolicy("CorsPolicy",
            builder =>
            {
                builder.AllowAnyMethod()
                .AllowAnyHeader()
                .AllowAnyOrigin()
                .AllowCredentials();
            }));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var supportedCultures = new[]
            {
                new CultureInfo("zh-CN"),
                new CultureInfo("en-US")
            };
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("zh-CN"),
                // Formatting numbers, dates, etc.
                SupportedCultures = supportedCultures,
                // UI strings that we have localized.
                SupportedUICultures = supportedCultures
            });
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            var queues = new[] { "default", "apis", "localjobs" };
            app.UseHangfireServer(new BackgroundJobServerOptions()
            {
                ServerTimeout = TimeSpan.FromMinutes(4),
                SchedulePollingInterval = TimeSpan.FromSeconds(1),//秒级任务需要配置短点，一般任务可以配置默认时间，默认15秒
                ShutdownTimeout = TimeSpan.FromMinutes(30),//超时时间
                Queues = ApiQueues,//队列
                WorkerCount = Math.Max(Environment.ProcessorCount, 40)//工作线程数，当前允许的最大线程，默认20
            }
            //,
            //服务器资源检测频率
            //additionalProcesses: new IBackgroundProcess[] { new   SystemMonitor(checkInterval: TimeSpan.FromSeconds(1)) }//new[] { new SystemMonitor(checkInterval: TimeSpan.FromSeconds(1))}
            );

            #region 后台进程

            if (HangfireSettings.Instance.UseBackWorker)
            {
                var listprocess = new List<IBackgroundProcess>
                {
                    new BackWorkers(HangfireSettings.Instance.backWorker)
                };
                app.UseHangfireServer(new BackgroundJobServerOptions()
                {
                    ServerName = $"{Environment.MachineName}-BackWorker",
                    WorkerCount = 20,
                    Queues = new[] { "test", "api", "demo" }
                }, additionalProcesses: listprocess);
            }

            #endregion

            app.UseHangfireDashboard("/job", new DashboardOptions
            {
                AppPath = HangfireSettings.Instance.AppWebSite,//返回时跳转的地址
                DisplayStorageConnectionString = false,//是否显示数据库连接信息
                IsReadOnlyFunc = Context =>
                {
                    return false;
                },
                Authorization = new[] { new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
                {
                    RequireSsl = false,//是否启用ssl验证，即https
                    SslRedirect = false,
                    LoginCaseSensitive = true,
                    Users = new []
                    {
                        new BasicAuthAuthorizationUser
                        {
                            Login =HangfireSettings.Instance.LoginUser,//登录账号
                            PasswordClear = HangfireSettings.Instance.LoginPwd//登录密码
                        }
                    }
                })
                }
            });
            //只读面板，只能读取不能操作
            app.UseHangfireDashboard("/job-read", new DashboardOptions
            {
                IgnoreAntiforgeryToken = true,
                AppPath = "#",//返回时跳转的地址
                DisplayStorageConnectionString = false,//是否显示数据库连接信息
                IsReadOnlyFunc = Context =>
                {
                    return true;
                },
                Authorization = new[] { new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
                {
                    RequireSsl = false,//是否启用ssl验证，即https
                    SslRedirect = false,
                    LoginCaseSensitive = true,
                    Users = new []
                    {
                        new BasicAuthAuthorizationUser
                        {
                            Login = "read",
                            PasswordClear = "only"
                        },
                        new BasicAuthAuthorizationUser
                        {
                            Login = "test",
                            PasswordClear = "123456"
                        },
                        new BasicAuthAuthorizationUser
                        {
                            Login = "guest",
                            PasswordClear = "123@123"
                        }
                    }
                })
                }
            });
            ////重写json报告数据，可用于远程调用获取健康检查结果
            //var options = new HealthCheckOptions
            //{
            //    ResponseWriter = async (c, r) =>
            //    {
            //        c.Response.ContentType = "application/json";

            //        var result = JsonConvert.SerializeObject(new
            //        {
            //            status = r.Status.ToString(),
            //            errors = r.Entries.Select(e => new { key = e.Key, value = e.Value.Status.ToString() })
            //        });
            //        await c.Response.WriteAsync(result);
            //    }
            //};

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<SignalrHubs>("/Hubs");
            });
        }
    }
}
