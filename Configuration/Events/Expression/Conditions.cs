﻿/* Copyright (C) 2021 Dylan Cheng (https://github.com/newlooper)

This file is part of VirtualSpace.

VirtualSpace is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

VirtualSpace is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with VirtualSpace. If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Forms;
using LinqExpressionBuilder;
using VirtualSpace.AppLogs;
using VirtualSpace.Commons;
using VirtualSpace.Config.Events.Entity;
using VirtualSpace.Helpers;
using Process = System.Diagnostics.Process;

namespace VirtualSpace.Config.Events.Expression
{
    public static partial class Conditions
    {
        private static readonly JsonParser                        Jp                        = new();
        private static          List<RuleTemplate>                _rules                    = InitRules();
        private static readonly Channel<Behavior>                 ActionProducer            = Channels.ActionChannel;
        private static readonly Channel<Window>                   VisibleWindowsConsumer    = Channels.VisibleWindowsChannel;
        private static readonly ConcurrentDictionary<IntPtr, int> WindowCheckTimes          = new();
        public static readonly  ConcurrentBag<IntPtr>             WndHandleIgnoreListByRule = new();

        static Conditions()
        {
            RuleChecker();
        }

        private static async void RuleChecker()
        {
            while ( await VisibleWindowsConsumer.Reader.WaitToReadAsync() )
            {
                if ( VisibleWindowsConsumer.Reader.TryRead( out var win ) )
                {
                    CheckRulesForWindow( win );
                }
            }
        }

        private static List<RuleTemplate> InitRules()
        {
            var path = Manager.GetRulesPath();
            _rules = new List<RuleTemplate>();
            if ( !File.Exists( path ) ) return _rules;

            _rules = FetchRuleList( path );

            return _rules;
        }

        private static void BuildRuleExp( List<RuleTemplate> rules )
        {
            foreach ( var rule in rules )
            {
                rule.Exp = Jp.ExpressionFromJsonDoc<Window>( rule.Expression );
            }
        }

        public static List<RuleTemplate> FetchRules()
        {
            return _rules;
        }

        public static async void CheckRulesForWindow( Window win )
        {
            if ( _rules.Count == 0 ) return;

            var rules = new List<RuleTemplate>( _rules );
            BuildRuleExp( rules );

            if ( !WindowCheckTimes.ContainsKey( win.Handle ) )
                WindowCheckTimes[win.Handle] = 0;

            var isOnePeriod = WindowCheckTimes[win.Handle] % Const.WindowCheckTimesLimit == 0;

            if ( isOnePeriod )
            {
                Logger.Debug( $"Checking rules for {win.Title}, current profile: {Manager.Configs.CurrentProfileName}" );
            }

            await Task.Run( () =>
            {
                _ = User32.GetWindowThreadProcessId( win.Handle, out var pId );
                using var pInfo = Process.GetProcessById( pId );

                win.ProcessName = pInfo.ProcessName;
                try
                {
                    win.ProcessPath = pInfo.MainModule?.FileName;
                    win.CommandLine = pInfo.GetCommandLineArgs();
                }
                catch ( Exception ex )
                {
                    Logger.Warning( "Get Process Info: " + ex.Message );
                }

                var screen      = Screen.FromHandle( win.Handle );
                var screenIndex = 0;
                var allScreens  = Screen.AllScreens;
                for ( var i = 0; i < allScreens.Length; i++ )
                {
                    if ( screen.DeviceName == allScreens[i].DeviceName )
                    {
                        screenIndex = i;
                        break;
                    }
                }

                win.WinInScreen = screenIndex.ToString();

                if ( !User32.IsWindow( win.Handle ) ) return;

                var hasMatchedRule = false;

                foreach ( var r in rules )
                {
                    if ( !r.Enabled ) continue;
                    var match = new List<Window> {win}.Where( r.Exp ).Any();
                    if ( match )
                    {
                        hasMatchedRule = true;
                        Logger.Debug( win.Title + $" match rule [{r.Name}]" );
                        r.Action.Handle = win.Handle;
                        r.Action.RuleName = r.Name;
                        r.Action.WindowTitle = win.Title;
                        ActionProducer.Writer.TryWrite( r.Action );

                        ////////////////////////////////////////////////////////////////
                        // 某个窗口可能与多条规则匹配，继续循环就表示所有相应的动作都会按顺序执行
                        // 但是，如果在此处退出循环，则表示仅执行第一条匹配规则的动作
                        // if ( OnlyRunActionOfFirstMatchedRule ) // <- 日后可能会引入选项来进行配置
                        // {
                        //     break;
                        // }
                    }
                }

                if ( hasMatchedRule )
                {
                    WndHandleIgnoreListByRule.Add( win.Handle );
                    return;
                }

                if ( isOnePeriod )
                {
                    Logger.Debug( $"Window [{win.Title}] has no matched rules." );
                }

                if ( Manager.CurrentProfile.IgnoreWindowOnRuleCheckTimeout )
                {
                    if ( WindowCheckTimes[win.Handle] >= Const.WindowCheckTimesLimit )
                    {
                        Logger.Debug( $"Try find rules for [{win.Title}] too many times, ignore the window." );
                        WndHandleIgnoreListByRule.Add( win.Handle );
                    }
                }

                WindowCheckTimes[win.Handle]++;
            } );
        }

        private static List<RuleTemplate> FetchRuleList( string path )
        {
            using var fs     = new FileStream( path, FileMode.Open, FileAccess.Read );
            var       buffer = new byte[fs.Length];
            _ = fs.Read( buffer, 0, (int)fs.Length );
            var utf8Reader = new Utf8JsonReader( buffer );

            var readOptions = new JsonSerializerOptions();
#if NETCOREAPP3_1 || NET5_0
            readOptions.Converters.Add( new JsonConverterJsonDocument() );
#endif
            return JsonSerializer.Deserialize<List<RuleTemplate>>( ref utf8Reader, readOptions );
        }

        public static ExpressionTemplate ParseExpressionTemplate( JsonDocument doc )
        {
            var readOptions = new JsonSerializerOptions();
#if NETCOREAPP3_1 || NET5_0
            readOptions.Converters.Add( new JsonConverterJsonDocument() );
            var json = JsonSerializer.Serialize( doc.RootElement, GetJsonSerializerOptions() );
            return JsonSerializer.Deserialize<ExpressionTemplate>( json, readOptions );
#elif NET6_0
            return JsonSerializer.Deserialize<ExpressionTemplate>( doc, readOptions );
#endif
        }

        public static JsonSerializerOptions GetJsonSerializerOptions()
        {
#if NETCOREAPP3_1 || NET5_0
            var writeOptions = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            writeOptions.Converters.Add( new JsonConverterJsonDocument() );
#elif NET6_0
            var writeOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
#endif
            return writeOptions;
        }

        public static async void SaveRules( string path, List<RuleTemplate> ruleList )
        {
            _rules = ruleList;
            await File.WriteAllBytesAsync( path, JsonSerializer.SerializeToUtf8Bytes(
                ruleList, GetJsonSerializerOptions() ) );

            Logger.Info( $"[RULE]Rules.{Manager.Configs.CurrentProfileName} Saved." );
        }
    }

#if NETCOREAPP3_1 || NET5_0
    internal sealed class JsonConverterJsonDocument : JsonConverter<JsonDocument>
    {
        public override JsonDocument Read( ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options )
        {
            return JsonDocument.ParseValue( ref reader );
        }

        public override void Write( Utf8JsonWriter writer, JsonDocument value, JsonSerializerOptions options )
        {
            value.RootElement.WriteTo( writer );
        }
    }
#endif
}