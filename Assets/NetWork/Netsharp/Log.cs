using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using UnityEngine.UI;
using UnityEngine;
namespace Crazy.ClientNet
{
    public static class Log
    {

        static GameObject content;
        static Log()
        {
             content = GameObject.Find("CanvasLog/Scroll View/Viewport/Content");

            
        }
        public static void Update()
        {
            
            
        }
        public static void Debug(string message)
        {
            dataLogs.Enqueue(message);
            UnityEngine.Debug.Log(message);
        }
        public static void Error(Exception e)
        {

            dataLogs.Enqueue(e.ToString());
            UnityEngine.Debug.Log(e.ToString());
        }
        public static void Error(string message)
        {
            dataLogs.Enqueue(message);
            UnityEngine.Debug.Log(message);
        }
        public static void Fatal(Exception e)
        {

          
            UnityEngine.Debug.Log(e.ToString());
            UnityEngine.Debug.Log(e.ToString());
        }
        public static void Fatal(string message)
        {
            dataLogs.Enqueue(message);
            UnityEngine.Debug.Log(message);
        }
        public static void Info(string message)
        {
            dataLogs.Enqueue(message);
            UnityEngine.Debug.Log(message);
        }
        public static void Msg(object message)
        {
            dataLogs.Enqueue(message.ToJson());
            UnityEngine.Debug.Log(message.ToJson());
        }
        public static void Trace(string message)
        {
            dataLogs.Enqueue(message);
            UnityEngine.Debug.Log(message);
        }
        public static void Warning(string message)
        {
            dataLogs.Enqueue(message);
            UnityEngine.Debug.Log(message);
        }
        public static Queue<string> dataLogs = new Queue<string>();
    }
}
