﻿using GorillaInfoWatch.Extensions;
using GorillaInfoWatch.Models;
using GorillaInfoWatch.Models.Attributes;
using GorillaInfoWatch.Tools;
using System;
using System.Reflection;

namespace GorillaInfoWatch
{
    public static class Notifications
    {
        internal static Action<Notification> RequestSendNotification;

        internal static Action<Notification, bool> RequestOpenNotification;

        public static void SendNotification(Notification notification)
        {
            if (notification is null) throw new ArgumentNullException(nameof(notification));

            Assembly assembly = Assembly.GetCallingAssembly();

            if (assembly != Assembly.GetExecutingAssembly() && assembly.GetCustomAttribute<InfoWatchCompatibleAttribute>() == null)
            {
                Logging.Warning($"Assembly {assembly.GetName().Name} is not permitted to send notifications");
                return;
            }

            Logging.Message($"Notification sending from {assembly.GetName().Name}:\n{notification.DisplayText}");

            RequestSendNotification?.SafeInvoke(notification);
        }

        public static void OpenNotification(Notification notification, bool process)
        {
            if (notification is null) throw new ArgumentNullException(nameof(notification));

            RequestOpenNotification?.SafeInvoke(notification, process);
        }
    }
}
