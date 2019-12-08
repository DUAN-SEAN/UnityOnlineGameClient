//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Crazy.Common;
//using Log = Crazy.ClientNet.Log;
//using MongoDB.Bson;
//using Crazy.ClientNet;

//namespace Crazy.Main
//{
//    /// <summary>
//    /// 创建队伍
//    /// </summary>
//    [MessageHandler]
//    public class S2C_CreateMatchTeamCompleteMessageHandler : AMHandler<S2C_CreateMatchTeamComplete>
//    {
//        protected override void Run(ISession playerContext, S2C_CreateMatchTeamComplete message)
//        {
//            PlayerContextBase pctx = playerContext as PlayerContextBase;
//            Log.Info(message.ToJson());
//            switch (message.State)
//            {
//                case S2C_CreateMatchTeamComplete.Types.State.Complete:
//                    Log.Info($"创建队伍成功 MatchId = {message.MatchTeamId}");
//                    var team = new MatchTeam(message.MatchTeamId);//默认为4
//                    pctx.CreateMatchTeam(team);
//                    team.Add(pctx.PlayerId);
//                    break;
//                case S2C_CreateMatchTeamComplete.Types.State.HaveTeam:
//                    Log.Info($"创建队伍失败 已经在队伍中了 MatchId = {message.MatchTeamId}");
//                    break;
//                case S2C_CreateMatchTeamComplete.Types.State.SystemError:
//                    Log.Info($"创建队伍成功 队伍系统错误不允许");
//                    break;
//                default: break;
//            }


//        }
//    }

//    /// <summary>
//    /// 退出队伍,可以是收到自己或者其他人退出
//    /// </summary>
//    [MessageHandler]
//    public class S2CM_ExitMatchTeamCompleteMessageHandler : AMHandler<S2CM_ExitMatchTeamComplete>
//    {
//        protected override void Run(ISession playerContext, S2CM_ExitMatchTeamComplete message)
//        {
//            PlayerContextBase playerctx = playerContext as PlayerContextBase;
//            Log.Info(message.ToJson());
//            switch (message.State)
//            {
//                case S2CM_ExitMatchTeamComplete.Types.State.Ok:

//                    if (message.MatchTeamId == playerctx.CurrentMatchTeamId)
//                    {
//                        if (message.LaunchPlayerId == playerctx.PlayerId)//是自己
//                        {
//                            playerctx.CloseMatchTeam();//关闭匹配队伍
//                            return;
//                        }
//                        else if (playerctx.CurrentMatchTeam.IsContain(message.LaunchPlayerId))//是别人
//                        {
//                            playerctx.CurrentMatchTeam.Remove(message.LaunchPlayerId);
//                        }
//                    }
//                    break;
//                case S2CM_ExitMatchTeamComplete.Types.State.Fail://无论如何 服务器最终都保证发起的玩家都不在服务器队伍中
//                    if (playerctx.CurrentMatchTeam == null) return;
//                    else
//                    {
//                        playerctx.CloseMatchTeam();
//                    }
//                    break;
//                default: break;
//            }
//        }
//    }
//    /// <summary>
//    /// 加入队伍成功 可以是自己也可以是别人
//    /// </summary>
//    [MessageHandler]
//    public class S2CM_JoinMatchTeamCompleteMessageHandler : AMHandler<S2CM_JoinMatchTeamComplete>
//    {
//        protected override void Run(ISession playerContext, S2CM_JoinMatchTeamComplete message)
//        {
//            Log.Info(message.ToJson());
//            PlayerContextBase playerctx = playerContext as PlayerContextBase;
//            switch (message.State)
//            {
//                case S2CM_JoinMatchTeamComplete.Types.State.Complete:
//                    if (playerctx.CurrentMatchTeam == null)//表示自己加入 因为房间为空
//                    {
//                        if (playerctx.PlayerId == message.LaunchPlayerId)//如果是自己发起的 就自己创建一个队伍
//                        {
//                            var team = new MatchTeam(message.MatchTeamId);
//                            playerctx.CreateMatchTeam(team);
//                            playerctx.CurrentMatchTeam.Add(message.LaunchPlayerId);
//                            Log.Info($"进入房间成功 MatchId = {message.MatchTeamId}");
//                            //TODO:还要请求服务器获取队伍里其他人的信息
//                        }
//                        else
//                        {
//                            Log.Info("虽然是我发起的进入房间的，但还是失败了");
//                            //错误消息
//                        }
//                    }
//                    else if (message.MatchTeamId == playerctx.CurrentMatchTeam.Id)//表示是收到其他玩家进入队伍的消息
//                    {
//                        if (playerctx.PlayerId != message.LaunchPlayerId)
//                        {
//                            //如果请求的队伍是本队伍 则加入此玩家到本队伍
//                            playerctx.CurrentMatchTeam.Add(message.LaunchPlayerId);
//                            Log.Info($"添加{message.LaunchPlayerId} 进入本队伍");

//                        }

//                    }
//                    else//表示是玩家加入的队伍
//                    {

//                    }
//                    break;
//                case S2CM_JoinMatchTeamComplete.Types.State.SystemError:
//                    Log.Info("服务器错误");
//                    break;
//                case S2CM_JoinMatchTeamComplete.Types.State.HaveTeam:
//                    Log.Info("我已经在队伍了");
//                    break;
//                default:
//                    break;
//            }
//        }
//    }
//    /// <summary>
//    /// 更新队伍信息
//    /// </summary>
//    [MessageHandler]
//    public class S2C_UpdateMatchTeamInfoMessageHandler : AMHandler<S2C_UpdateMatchTeamInfo>
//    {
//        /// <summary>
//        /// 刷新队伍信息
//        /// </summary>
//        /// <param name="playerContext"></param>
//        /// <param name="message"></param>
//        protected override void Run(ISession playerContext, S2C_UpdateMatchTeamInfo message)
//        {
//            PlayerContextBase playerctx = playerContext as PlayerContextBase;
//            Log.Info(message.ToJson());

//            MatchTeam matchTeam = playerctx.CurrentMatchTeam;
//            if (matchTeam == null) return;
//            if (matchTeam.Id == message.MatchTeamId)
//            {
//                //1 清空一边集合
//                matchTeam.Clear();
//                foreach (var id in message.TeamInfo.PlayerIds)
//                {
//                    matchTeam.Add(id);
//                }
//                //2TODO 重新添加 刷新队伍信息，显示在UI界面上

//            }
//        }
//    }
//    /// <summary>
//    /// 加入匹配队列成功，队伍里的所有人都会收到这个请求
//    /// </summary>
//    [MessageHandler]
//    public class S2C_JoinMatchQueueCompleteMessageHandler : AMHandler<S2CM_JoinMatchQueueComplete>
//    {
//        protected override void Run(ISession playerContext, S2CM_JoinMatchQueueComplete message)
//        {
//            PlayerContextBase playerctx = playerContext as PlayerContextBase;
//            Log.Info(message.ToJson());
//            switch (message.State)
//            {
//                case S2CM_JoinMatchQueueComplete.Types.State.Ok:
//                    MatchTeam matchTeam = playerctx.CurrentMatchTeam;
//                    if (matchTeam == null) return;
//                    if (matchTeam.Id == message.MatchTeamId)
//                    {
//                        Log.Info("加入匹配队列成功\n进入的关卡Id = " + message.BarrierId);
//                        matchTeam.State = MatchTeam.MatchTeamState.Matching;
//                    }

//                    break;
//                case S2CM_JoinMatchQueueComplete.Types.State.Fail:
//                    break;
//                default:
//                    break;
//            }
//        }
//    }
//    /// <summary>
//    /// 退出匹配队列
//    /// </summary>
//    [MessageHandler]
//    public class S2CM_ExitMatchQueueMessageHandler : AMHandler<S2CM_ExitMatchQueue>
//    {
//        protected override void Run(ISession playerContext, S2CM_ExitMatchQueue message)
//        {
//            PlayerContextBase playerctx = playerContext as PlayerContextBase;
//            Log.Info(message.ToJson());
//            if (playerctx.CurrentMatchTeam == null)
//                return;
//            if (playerctx.CurrentMatchTeam.Id == message.MatchTeamId)//表示需要退出队伍
//            {
//                Log.Info("退出匹配队列成功\n" + (message.State == S2CM_ExitMatchQueue.Types.State.Client ? "客户发起" : "服务器发起的"));
//                playerctx.CurrentMatchTeam.State = MatchTeam.MatchTeamState.OPEN;
//            }
//        }
//    }
//}
