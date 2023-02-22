﻿/* Copyright (C) 2021 Dylan Cheng (https://github.com/newlooper)

This file is part of VirtualSpace.

VirtualSpace is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

VirtualSpace is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with VirtualSpace. If not, see <https://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using VirtualSpace.Config.Entity;

namespace VirtualSpace.Config
{
    public class ConfigTemplate
    {
        public Dictionary<string, Profile> Profiles           { get; set; }
        public string                      CurrentProfileName { get; set; }
        public string                      Version            { get; set; }
        public LogConfig                   LogConfig          { get; set; }

        public Dictionary<string, KeyBinding>? KeyBindings { get; set; } = new()
        {
            {Const.Hotkey.RISE_VIEW, new KeyBinding {GhkCode = "_+Ctrl+_+Shift+Tab", MessageId = Const.Hotkey.Info[Const.Hotkey.RISE_VIEW].Item2}},
            {Const.Hotkey.RISE_VIEW_FOR_ACTIVE_APP, new KeyBinding {GhkCode = "", MessageId = Const.Hotkey.Info[Const.Hotkey.RISE_VIEW_FOR_ACTIVE_APP].Item2}},
            {Const.Hotkey.SHOW_APP_CONTROLLER, new KeyBinding {GhkCode = "_+Ctrl+Alt+_+F12", MessageId = Const.Hotkey.Info[Const.Hotkey.SHOW_APP_CONTROLLER].Item2}},
            {Const.Hotkey.NAV_LEFT, new KeyBinding {GhkCode = "", MessageId = Const.Hotkey.Info[Const.Hotkey.NAV_LEFT].Item2}},
            {Const.Hotkey.NAV_RIGHT, new KeyBinding {GhkCode = "", MessageId = Const.Hotkey.Info[Const.Hotkey.NAV_RIGHT].Item2}},
            {Const.Hotkey.NAV_UP, new KeyBinding {GhkCode = "", MessageId = Const.Hotkey.Info[Const.Hotkey.NAV_UP].Item2}},
            {Const.Hotkey.NAV_DOWN, new KeyBinding {GhkCode = "", MessageId = Const.Hotkey.Info[Const.Hotkey.NAV_DOWN].Item2}}
        };

        public Dictionary<string, Const.MouseAction.Action>? MouseActions { get; set; } = DefaultMouseActions();

        public Cluster Cluster { get; set; } = new()
        {
            HideMainViewIfItsShown = false,
            NotificationOnVdChanged = false,
            ShowVDIndexOnTrayIcon = false,
            HideOnStart = false
        };

        public Const.MouseAction.Action GetMouseActionById( string id )
        {
            if ( MouseActions == null || MouseActions.Count == 0 )
            {
                MouseActions = DefaultMouseActions();
            }

            return MouseActions.ContainsKey( id )
                ? MouseActions[id]
                : Const.MouseAction.Action.DoNothing;
        }

        private static Dictionary<string, Const.MouseAction.Action> DefaultMouseActions()
        {
            return new Dictionary<string, Const.MouseAction.Action>
            {
                {Const.MouseAction.DESKTOP_LEFT_CLICK, Const.MouseAction.Action.DesktopVisibleAndCloseView},
                {Const.MouseAction.DESKTOP_MIDDLE_CLICK, Const.MouseAction.Action.DesktopVisibleOnly},
                {Const.MouseAction.DESKTOP_RIGHT_CLICK, Const.MouseAction.Action.ContextMenu},

                {Const.MouseAction.WINDOW_LEFT_CLICK, Const.MouseAction.Action.WindowActiveDesktopVisibleAndCloseView},
                {Const.MouseAction.WINDOW_MIDDLE_CLICK, Const.MouseAction.Action.WindowActiveDesktopVisibleOnly},
                {Const.MouseAction.WINDOW_RIGHT_CLICK, Const.MouseAction.Action.ContextMenu},

                {Const.MouseAction.WINDOW_CTRL_LEFT_CLICK, Const.MouseAction.Action.DoNothing},
                {Const.MouseAction.WINDOW_CTRL_MIDDLE_CLICK, Const.MouseAction.Action.DoNothing},
                {Const.MouseAction.WINDOW_CTRL_RIGHT_CLICK, Const.MouseAction.Action.DoNothing},

                {Const.MouseAction.WINDOW_ALT_LEFT_CLICK, Const.MouseAction.Action.DoNothing},
                {Const.MouseAction.WINDOW_ALT_MIDDLE_CLICK, Const.MouseAction.Action.DoNothing},
                {Const.MouseAction.WINDOW_ALT_RIGHT_CLICK, Const.MouseAction.Action.DoNothing},

                {Const.MouseAction.WINDOW_SHIFT_LEFT_CLICK, Const.MouseAction.Action.DoNothing},
                {Const.MouseAction.WINDOW_SHIFT_MIDDLE_CLICK, Const.MouseAction.Action.DoNothing},
                {Const.MouseAction.WINDOW_SHIFT_RIGHT_CLICK, Const.MouseAction.Action.DoNothing}
            };
        }
    }
}