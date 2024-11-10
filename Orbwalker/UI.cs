using Dalamud.Game.ClientState.GamePad;
using Dalamud.Interface.Components;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.Gamepad;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;
using Orbwalker;
using PunishLib;
using PunishLib.ImGuiMethods;
using System.IO;
using System.Windows.Forms;
using ThreadLoadImageHandler = ECommons.ImGuiMethods.ThreadLoadImageHandler;

namespace Orbwalker
{
    internal unsafe static class UI
    {
        internal static void Draw()
        {
            ImGuiEx.EzTabBar("Default",
                ("设置", Settings, null, true),
                ("职业", Jobs, null, true),
                ("额外", Extras, null, true),
                ("关于", () =>
                {
                    ImGuiEx.LineCentered("本地化", delegate
                    {
                        ImGuiEx.Text("本地化: KirisameVanilla");
                    });
                    AboutTab.Draw(P.Name);
                }, null, true),
                ("Debug".NullWhenFalse(C.Debug), Debug, ImGuiColors.DalamudGrey3, true),
                InternalLog.ImGuiTab(C.Debug)

                );
        }

        static void Extras()
        {
            ImGui.TextColored(new(1f,0f,0f,1f),"你仍然需要在职业中选择对应职业");
            ImGuiEx.Text($"职业额外选项");
            ImGuiGroup.BeginGroupBox();
            ImGuiEx.Text($"在使用以下技能时启用滑步锁:");
            ImGuiEx.Spacing(); ImGui.Checkbox("武装戍卫（骑士）", ref C.PreventPassage);
            ImGuiEx.Spacing(); ImGui.Checkbox("天地人（忍者）", ref C.PreventTCJ);
            ImGuiEx.Spacing(); ImGui.Checkbox("火焰喷射器（机工士）", ref C.PreventFlame);
            ImGuiEx.Spacing(); ImGui.Checkbox("即兴表演（舞者）", ref C.PreventImprov);
			ImGuiEx.Spacing(); ImGui.Checkbox("鬼人乱舞（鬼宿脚）（青魔法师）", ref C.PreventPhantom);
			ImGuiGroup.EndGroupBox();


            ImGuiEx.Text($"各种读条");
            ImGuiGroup.BeginGroupBox();
            ImGuiEx.Text($"在使用以下技能时启用滑步锁:");
            ImGuiEx.Spacing(); ImGui.Checkbox("传送", ref C.BlockTP);
            ImGuiEx.Spacing(); ImGui.Checkbox("返回", ref C.BlockReturn);
            ImGuiEx.Spacing(); ImGui.Checkbox("坐骑", ref C.BlockMount);
            ImGuiGroup.EndGroupBox();

            ImGuiEx.Text($"PvP设置");
            ImGuiGroup.BeginGroupBox();
            ImGui.Checkbox($"在PvP中使用滑步", ref C.PVP);
            ImGuiComponents.HelpMarker("在PvP模式中（如纷争前线和水晶冲突）启用滑步锁。使用后果自负。");
            ImGuiGroup.EndGroupBox();
        }

        static void Spacing(bool cont = false)
        {
            ImGuiEx.TextV($" {(cont ? "├" : "└")} ");
            ImGui.SameLine();
        }

        static void Settings()
        {
            var cur = ImGui.GetCursorPos();
            if (ThreadLoadImageHandler.TryGetTextureWrap(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, "res", "q.png"), out var t))
            {
                ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X - 20);
                ImGui.Image(t.ImGuiHandle, new(20, 20));
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    if (ThreadLoadImageHandler.TryGetTextureWrap(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, "res", "t.png"), out var t2))
                    {
                        ImGui.Image(t2.ImGuiHandle, new Vector2(t2.Width, t2.Height));
                    }
                    ImGui.EndTooltip();
                    if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Right))
                    {
                        C.Debug = !C.Debug;
                    }
                }
            }
            ImGui.SetCursorPos(cur);

            if (ImGui.Checkbox($"启用", ref C.Enabled))
            {
                P.Memory.EnableDisableBuffer();
            }
            ImGuiEx.Text($"移动");
            ImGuiGroup.BeginGroupBox();
            ImGuiEx.Text($"滑步窗口校准:");
            ImGuiComponents.HelpMarker("切换校准滑步窗口的模式。自动模式较为保守，校准的滑步窗口比根据网络延迟手动调节的要延后一点。");
            Spacing(!C.IsSlideAuto);
            ImGuiEx.RadioButtonBool("自动", "手动", ref C.IsSlideAuto, true);
            if (!C.IsSlideAuto)
            {
                Spacing();
                ImGui.SetNextItemWidth(200f);
                ImGui.SliderFloat("滑步锁阈值, 秒", ref C.Threshold, 0.1f, 1f);
            }
            ImGuiEx.Text($"滑步模式");
            ImGuiComponents.HelpMarker("在两种滑步锁模式中切换。\n" +
                                       "\"Slidecast\"是默认模式，它只是简单地阻止玩家在非滑步窗口期的读条中的移动，直到滑步窗口。多数情况下，在第一次读条中，你必须保持静止。" +
                                       "\"Slidelock\"模式则是一直禁用玩家在战斗中的移动，只允许在滑步窗口中移动。启用该模式后，按下移动的释放键是唯一能够启用移动的方法。");
            Spacing();
            if (ImGui.RadioButton("Slidecast", !C.ForceStopMoveCombat))
            {
                C.ForceStopMoveCombat = false;
            }
            ImGui.SameLine();
            if (ImGui.RadioButton("Slidelock", C.ForceStopMoveCombat))
            {
                C.ForceStopMoveCombat = true;
            }

            if (ImGui.Checkbox($"缓存第一次读条", ref C.Buffer))
            {
                P.Memory.EnableDisableBuffer();
            }
            ImGuiComponents.HelpMarker($"通过缓存第一次读条直到移动停止，削除了玩家在引导第一次读条时保持静止的要求。此设置可能会导致 Redirect 或 ReAction 等插件出现奇怪的行为，或导致其选项完全无法工作，请注意！");

            ImGui.SetNextItemWidth(200f);
            ImGui.Checkbox("手柄模式", ref C.ControllerMode);

            if (C.ControllerMode)
                DrawKeybind("移动的释放按钮", ref C.ReleaseButton);
            else
                DrawKeybind("移动的释放键", ref C.ReleaseKey);
            ImGuiComponents.HelpMarker("绑定一个按键即可立即解锁玩家移动并取消任何引导施法。请注意，只有按住按键时才可以移动，因此建议使用鼠标按钮。");
            ImGui.Checkbox($"永久释放", ref C.UnlockPermanently);
            ImGuiComponents.HelpMarker("释放玩家的移动——该设置主要由上方的释放键使用");
            ImGuiEx.Text($"释放键模式:");
            ImGuiComponents.HelpMarker("在按住和点击切换中切换。");
            Spacing();
            ImGuiEx.RadioButtonBool("按住", "点击切换", ref C.IsHoldToRelease, true);

            if (!C.ControllerMode)
            {
                ImGui.Checkbox($"通过鼠标按键释放", ref C.DisableMouseDisabling);
                ImGuiComponents.HelpMarker("紧急情况下同时按住鼠标左键和右键来进行移动");
                ImGuiEx.TextV($"移动的方向键:");
                ImGui.SameLine();
                ImGuiEx.SetNextItemWidth(0.8f);
                if (ImGui.BeginCombo($"##movekeys", $"{C.MoveKeys.Print()}"))
                {
                    foreach (var x in Svc.KeyState.GetValidVirtualKeys())
                    {
                        ImGuiEx.CollectionCheckbox($"{x}", x, C.MoveKeys);
                    }
                    ImGui.EndCombo();
                }
            }

            ImGui.PushItemWidth(300);
            ImGuiEx.TextV($"保持地面指向性读条 (seconds):");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100f);

            if (ImGui.InputFloat("###GroundedHoldConfig", ref P.Config.GroundedHold,0,0,"%.2g"))
            {
                if (P.Config.GroundedHold <= 0)
                    P.Config.GroundedHold = 0;

                if (P.Config.GroundedHold % 0.01f != 0)
                    P.Config.GroundedHold = (float)Math.Round(P.Config.GroundedHold, 2, MidpointRounding.ToNegativeInfinity);
            }

            ImGuiComponents.HelpMarker($"如果读条以地面为目标（主要是青魔法师），则当目标出现时会记录施法时间，如果确认移动，则会取消施法。 " +
                $"该选项会强制您的角色在目标出现时停止，只要不提前取消施法。");

            ImGuiGroup.EndGroupBox();

            ImGuiEx.Text($"外置UI");
            ImGuiGroup.BeginGroupBox();

            ImGuiEx.Text($"展示外置UI");
            ImGuiComponents.HelpMarker("选择展示UI的条件");
            Spacing(true); ImGui.Checkbox($"在战斗中", ref C.DisplayBattle);
            Spacing(true); ImGui.Checkbox($"在任务中", ref C.DisplayDuty);
            Spacing(true); ImGui.Checkbox($"常驻", ref C.DisplayAlways);
            Spacing();
            ImGui.SetNextItemWidth(100f);
            ImGui.SliderFloat($"外置UI缩放比例", ref C.SizeMod.ValidateRange(0.5f, 2f), 0.8f, 1.2f);

            ImGuiGroup.EndGroupBox();
        }

        static void Jobs()
        {
            ImGuiEx.Text($"职业");
            ImGuiComponents.HelpMarker("选择您希望使用滑步锁定的职业。并非所有职业都有读条技能，但如果您为一些技能启用了额外功能（如忍者的天地人），这些技能的滑步锁也会启用。");
            ImGuiGroup.BeginGroupBox();
            ImGuiEx.TextV("一键启用/禁用:");
            ImGui.SameLine();
            if (ImGui.Button("所有职业"))
            {
                var jobs = Enum.GetValues<Job>().Where(x => x != Job.ADV);
                var b = jobs.All(x => C.EnabledJobs.TryGetValue(x, out var v) && v);
                foreach (var x in jobs)
                {
                    C.EnabledJobs[x] = !b;
                }
            }
            ImGui.SameLine();
            if (ImGui.Button($"有咏唱的职业"))
            {
                var b = Data.CastingJobs.All(x => C.EnabledJobs.TryGetValue(x, out var v) && v);
                foreach (var x in Data.CastingJobs)
                {
                    C.EnabledJobs[x] = !b;
                }
            }
            /*
            ImGui.SameLine();
            if (ImGui.Button("生采职业"))
            {
                var jobs = Svc.Data.GetExcelSheet<ClassJob>().Where(x => x.ClassJobCategory.Value.RowId.EqualsAny<uint>(33, 32)).Select(x => x.RowId).Cast<Job>();
                var b = jobs.All(x => C.EnabledJobs.TryGetValue(x, out var v) && v);
                foreach (var x in jobs)
                {
                    C.EnabledJobs[x] = !b;
                }
            }
            */
            ImGui.Separator();
            ImGui.Columns(5, "###JobGrid", false);
            foreach (var job in Enum.GetValues<Job>())
            {
                if (job == Job.ADV) continue;
                if (!P.Config.EnabledJobs.ContainsKey(job))
                    P.Config.EnabledJobs[job] = false;

                if (!JobNames.ContainsKey(job) || (JobNames.ContainsKey(job) && JobNames[job] == "")) continue;
                bool val = P.Config.EnabledJobs[job];
                if (ImGui.Checkbox($"{JobNames[job]}", ref val))
                {
                    if (val)
                    {
                        switch (job)
                        {
                            case Job.WHM:
                            case Job.CNJ:
                                P.Config.EnabledJobs[Job.WHM] = val;
                                P.Config.EnabledJobs[Job.CNJ] = val;
                                break;
                            case Job.BLM:
                            case Job.THM:
                                P.Config.EnabledJobs[Job.BLM] = val;
                                P.Config.EnabledJobs[Job.THM] = val;
                                break;
                            case Job.MNK:
                            case Job.PGL:
                                P.Config.EnabledJobs[Job.MNK] = val;
                                P.Config.EnabledJobs[Job.PGL] = val;
                                break;
                            case Job.ACN:
                            case Job.SMN:
                            case Job.SCH:
                                P.Config.EnabledJobs[Job.ACN] = val;
                                P.Config.EnabledJobs[Job.SMN] = val;
                                P.Config.EnabledJobs[Job.SCH] = val;
                                break;
                            case Job.MRD:
                            case Job.WAR:
                                P.Config.EnabledJobs[Job.MRD] = val;
                                P.Config.EnabledJobs[Job.WAR] = val;
                                break;
                            case Job.PLD:
                            case Job.GLA:
                                P.Config.EnabledJobs[Job.PLD] = val;
                                P.Config.EnabledJobs[Job.GLA] = val;
                                break;
                            case Job.ROG:
                            case Job.NIN:
                                P.Config.EnabledJobs[Job.ROG] = val;
                                P.Config.EnabledJobs[Job.NIN] = val;
                                break;
                            case Job.BRD:
                            case Job.ARC:
                                P.Config.EnabledJobs[Job.BRD] = val;
                                P.Config.EnabledJobs[Job.ARC] = val;
                                break;
                            case Job.LNC:
                            case Job.DRG:
                                P.Config.EnabledJobs[Job.LNC] = val;
                                P.Config.EnabledJobs[Job.DRG] = val;
                                break;
                            case Job.CUL:
                            case Job.ALC:
                            case Job.BSM:
                            case Job.GSM:
                            case Job.ARM:
                            case Job.LTW:
                            case Job.CRP:
                            case Job.WVR:
                                P.Config.EnabledJobs[Job.CUL] = val;
                                P.Config.EnabledJobs[Job.ALC] = val;
                                P.Config.EnabledJobs[Job.BSM] = val;
                                P.Config.EnabledJobs[Job.GSM] = val;
                                P.Config.EnabledJobs[Job.ARM] = val;
                                P.Config.EnabledJobs[Job.LTW] = val;
                                P.Config.EnabledJobs[Job.CRP] = val;
                                P.Config.EnabledJobs[Job.WVR] = val;
                                break;
                            case Job.BTN:
                            case Job.MIN:
                            case Job.FSH:
                                P.Config.EnabledJobs[Job.BTN] = val;
                                P.Config.EnabledJobs[Job.MIN] = val;
                                P.Config.EnabledJobs[Job.FSH] = val;
                                break;
                        }
                    }

                    P.Config.EnabledJobs[job] = val;
                }
                ImGui.NextColumn();
            }
            ImGui.Columns(1);
            ImGuiGroup.EndGroupBox();
        }

        static void Debug()
        {
            //ImGui.InputInt($"forceDisableMovementPtr", ref P.Memory.ForceDisableMovement);
            if (Svc.Targets.Target != null)
            {
                var addInfo = stackalloc uint[1];
                // was spell before, fixed
                ImGuiEx.Text($"{ActionManager.Instance()->GetActionStatus(ActionType.Action, 16541, Svc.Targets.Target.Struct()->EntityId, outOptExtraInfo: addInfo)} / {*addInfo}");
            }
            ImGuiEx.Text($"GCD: {Util.GCD}\nRCorGRD:{Util.GetRCorGDC()}");
        }

        static string KeyInputActive = null;
        static bool DrawKeybind(string text, ref Keys key)
        {
            bool ret = false;
            ImGui.PushID(text);
            ImGuiEx.Text($"{text}:");
            ImGui.Dummy(new(20, 1));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200f);
            if (ImGui.BeginCombo("##inputKey", $"{key}"))
            {
                if (text == KeyInputActive)
                {
                    ImGuiEx.Text(ImGuiColors.DalamudYellow, $"等待按下按键...");
                    foreach (var x in Enum.GetValues<Keys>())
                    {
                        if (IsKeyPressed((int) x))
                        {
                            KeyInputActive = null;
                            key = x;
                            ret = true;
                            break;
                        }
                    }
                }
                else
                {
                    if (ImGui.Selectable("自动检测新按键", false, ImGuiSelectableFlags.DontClosePopups))
                    {
                        KeyInputActive = text;
                    }
                    ImGuiEx.Text($"手动选择按键:");
                    ImGuiEx.SetNextItemFullWidth();
                    ImGuiEx.EnumCombo("##selkeyman", ref key);
                }
                ImGui.EndCombo();
            }
            else
            {
                if (text == KeyInputActive)
                {
                    KeyInputActive = null;
                }
            }
            if (key != Keys.None)
            {
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                {
                    key = Keys.None;
                    ret = true;
                }
            }
            ImGui.PopID();
            return ret;
        }

        static bool DrawKeybind(string text, ref GamepadButtons key)
        {
            bool ret = false;
            ImGui.PushID(text);
            ImGuiEx.Text($"{text}:");
            ImGui.Dummy(new(20, 1));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200f);
            if (ImGui.BeginCombo("##inputKey", $"{GamePad.ControllerButtons[key]}"))
            {
                if (text == KeyInputActive)
                {
                    ImGuiEx.Text(ImGuiColors.DalamudYellow, $"等待按下按键...");
                    foreach (var x in GamePad.ControllerButtons)
                    {
                        if (GamePad.IsButtonPressed(x.Key))
                        {
                            KeyInputActive = null;
                            key = x.Key;
                            ret = true;
                            break;
                        }
                    }
                }
                else
                {
                    if (ImGui.Selectable("自动检测新按键", false, ImGuiSelectableFlags.DontClosePopups))
                    {
                        KeyInputActive = text;
                    }
                    ImGuiEx.Text($"手动选择按键:");
                    ImGuiEx.SetNextItemFullWidth();
                    if (ImGui.BeginCombo("##selkeyman", GamePad.ControllerButtons[key]))
                    {
                        foreach (var button in GamePad.ControllerButtons)
                        {
                            if (ImGui.Selectable($"{button.Value}", button.Key == key))
                                key = button.Key;
                        }

                        ImGui.EndCombo();
                    }
                }
                ImGui.EndCombo();
            }
            else
            {
                if (text == KeyInputActive)
                {
                    KeyInputActive = null;
                }
            }
            if (key != GamepadButtons.None)
            {
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                {
                    key = GamepadButtons.None;
                    ret = true;
                }
            }
            ImGui.PopID();
            return ret;
        }

        private static readonly Dictionary<Job, string> JobNames = new()
        {
            { Job.GLA, "剑术师" },
            { Job.PGL, "格斗家" },
            { Job.MRD, "斧术师" },
            { Job.LNC, "枪术师" },
            { Job.ARC, "弓箭手" },
            { Job.CNJ, "幻术师"},
            { Job.THM, "咒术师"},
            { Job.CRP, ""},
            { Job.BSM, ""},
            { Job.ARM, ""},
            { Job.GSM, ""},
            { Job.LTW, ""},
            { Job.WVR, ""},
            { Job.ALC, ""},
            { Job.CUL, ""},
            { Job.MIN, ""},
            { Job.BTN, ""},
            { Job.FSH, ""},
            { Job.PLD, "骑士"},
            { Job.MNK, "武僧"},
            { Job.WAR, "战士"},
            { Job.DRG, "龙骑士"},
            { Job.BRD, "吟游诗人"},
            { Job.WHM, "白魔法师"},
            { Job.BLM, "黑魔法师"},
            { Job.ACN, "秘术师"},
            { Job.SMN, "召唤师"},
            { Job.SCH, "学者"},
            { Job.ROG, "双剑师"},
            { Job.NIN, "忍者"},
            { Job.MCH, "机工士"},
            { Job.DRK, "暗黑骑士"},
            { Job.AST, "占星术士"},
            { Job.SAM, "武士"},
            { Job.RDM, "赤魔法师"},
            { Job.BLU, "青魔法师"},
            { Job.GNB, "绝枪战士"},
            { Job.DNC, "舞者"},
            { Job.RPR, "钐镰客"},
            { Job.SGE, "贤者"},
            { Job.VPR, "蝰蛇武士"},
            { Job.PCT, "绘灵法师"}
        };
    }
}
