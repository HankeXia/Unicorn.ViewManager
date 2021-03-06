﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using System.Windows.Threading;

namespace Unicorn.ViewManager
{
    public static class ViewCommands
    {
        //static ViewCommands()
        //{
        //    DispatcherTimer dt = new DispatcherTimer(DispatcherPriority.Input)
        //    {
        //        Interval = TimeSpan.FromMilliseconds(500)
        //    };
        //    dt.Tick += (sender, e) =>
        //    {
        //        CommandManager.InvalidateRequerySuggested();
        //    };
        //    dt.Start();
        //}

        private enum CommandId : byte
        {
            ShowPopupItem,
            ClosePopupItem,

            ShowView,
            CloseView,
            SwitchView,

            Last
        }

        private static RoutedUICommand[] _internalCommands = new RoutedUICommand[(int)CommandId.Last];

        public static RoutedUICommand ShowPopupItem
        {
            get
            {
                return _EnsureCommand(CommandId.ShowPopupItem);
            }
        }

        public static RoutedUICommand ClosePopupItem
        {
            get
            {
                return _EnsureCommand(CommandId.ClosePopupItem);
            }
        }

        public static RoutedUICommand ShowView
        {
            get
            {
                return _EnsureCommand(CommandId.ShowView);
            }
        }

        public static RoutedUICommand CloseView
        {
            get
            {
                return _EnsureCommand(CommandId.CloseView);
            }
        }

        public static RoutedUICommand SwitchView
        {
            get
            {
                return _EnsureCommand(CommandId.SwitchView);
            }
        }

        private static RoutedUICommand _EnsureCommand(CommandId idCommand)
        {
            lock (_internalCommands.SyncRoot)
            {
                if (_internalCommands[(int)idCommand] == null)
                {
                    _internalCommands[(int)idCommand] = new RoutedUICommand(
                            GetUIText(idCommand),
                            GetCommandName(idCommand),
                            typeof(ViewCommands)
                        );
                }
            }
            return _internalCommands[(int)idCommand];
        }

        private static string GetUIText(CommandId commandId)
        {
            return commandId.ToString();
        }

        private static string GetCommandName(CommandId commandId)
        {
            return commandId.ToString();
        }
    }
}
