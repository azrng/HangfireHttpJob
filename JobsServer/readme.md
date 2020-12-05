# 说明

## 来源
本项目是Copy自 https://github.com/gnsilence/HangfireHttpJob  ,基本代码都是来源自这个项目，然后经过本人修改、简单化，然后作为本人学习使用的NET5项目。

## 简单的介绍
>拉取项目  
>运行项目然后访问http://localhost:9006/job 输入密码访问可以操作的控制面板  
>运行项目然后访问http://localhost:9006/job-read输入密码进入只读的控制面板  


## 操作事例
### 添加定时任务的事例
``` c#
{
  "Method": "GET",
  "ContentType": "application/json",
  "Url": "请求地址",
  "Data": {},
  "Timeout": 900,
  "Corn": "0 0/1 * * * ?",
  "BasicUserName": "",
  "BasicPassword": "",
  "QueueName": "apis",
  "JobName": "爬取段子详情",
  "IsRetry": false
}
```


## 注意事项
1.不能直接修改代码的方式，直接在里面写死任务，本项目是作为一个任务平台进行使用的。(如果这样子的话会在查询任务详情的时候报错)

## 个人说明
本人有意愿想为DOTNET社区发展做出一点力所能及的贡献