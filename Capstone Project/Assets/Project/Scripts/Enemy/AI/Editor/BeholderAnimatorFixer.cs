#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Mhieu.Enemy.EditorTools
{
    /// <summary>
    /// Sửa Animator Controller của Beholder (quái tầm xa) để animation do GAME điều khiển.
    /// Controller gốc là bản trình diễn nối chuỗi mọi state, không có parameter.
    ///  1. Xóa transition cũ (giữ state và clip). Mặc định: "IdleBattle" (state gốc, không phải "IdleBattle 0/1").
    ///  2. Parameter: Speed (float), Attack (trigger), AttackId (int), Hit (trigger), Die (trigger).
    ///  3. IdleBattle &lt;-&gt; Run theo Speed; Any -> Attack01 (AttackId = 0), Any -> Attack03 (AttackId &gt; 0),
    ///     Any -> GetHit (Hit), Any -> Die (Die); Attack01 / Attack03 / GetHit quay về IdleBattle khi xong.
    /// Menu: Tools > Enemy > Sửa Animator Beholder. Nên nhân đôi Beholder.controller để dự phòng trước.
    /// </summary>
    public static class BeholderAnimatorFixer
    {
        [MenuItem("Tools/Enemy/Sửa Animator Beholder")]
        public static void Fix()
        {
            AnimatorController controller = Selection.activeObject as AnimatorController;
            if (controller == null) controller = FindController();
            if (controller == null)
            {
                EditorUtility.DisplayDialog("Sửa Animator Beholder",
                    "Không tìm thấy Beholder.controller. Hãy chọn file đó trong Project rồi chạy lại.", "OK");
                return;
            }

            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState idle = FindState(machine, "IdleBattle");
            AnimatorState run = FindState(machine, "Run");
            AnimatorState attack01 = FindState(machine, "Attack01");
            AnimatorState attack03 = FindState(machine, "Attack03");
            AnimatorState getHit = FindState(machine, "GetHit");
            AnimatorState die = FindState(machine, "Die");

            if (idle == null || run == null || attack01 == null || attack03 == null || getHit == null || die == null)
            {
                EditorUtility.DisplayDialog("Sửa Animator Beholder",
                    "Controller '" + controller.name + "' thiếu state. Cần đủ: IdleBattle, Run, Attack01, Attack03, GetHit, Die.", "OK");
                return;
            }

            foreach (ChildAnimatorState child in machine.states)
                foreach (AnimatorStateTransition old in new List<AnimatorStateTransition>(child.state.transitions))
                    child.state.RemoveTransition(old);
            foreach (AnimatorStateTransition old in new List<AnimatorStateTransition>(machine.anyStateTransitions))
                machine.RemoveAnyStateTransition(old);

            machine.defaultState = idle;

            EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "AttackId", AnimatorControllerParameterType.Int);
            EnsureParameter(controller, "Hit", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Die", AnimatorControllerParameterType.Trigger);

            AnimatorStateTransition toRun = idle.AddTransition(run);
            SetupBlend(toRun);
            toRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            AnimatorStateTransition toIdle = run.AddTransition(idle);
            SetupBlend(toIdle);
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            AnimatorStateTransition a1 = AddAny(machine, attack01);
            a1.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            a1.AddCondition(AnimatorConditionMode.Equals, 0f, "AttackId");

            AnimatorStateTransition a3 = AddAny(machine, attack03);
            a3.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            a3.AddCondition(AnimatorConditionMode.Greater, 0f, "AttackId");

            AnimatorStateTransition hit = AddAny(machine, getHit);
            hit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");

            AnimatorStateTransition dead = AddAny(machine, die);
            dead.AddCondition(AnimatorConditionMode.If, 0f, "Die");
            dead.canTransitionToSelf = false;

            AddReturn(attack01, idle);
            AddReturn(attack03, idle);
            AddReturn(getHit, idle);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log("[BeholderAnimatorFixer] Đã sửa '" + controller.name +
                                  "'. Kiểm tra: clip Die tắt Loop Time; Animator trên model tắt Apply Root Motion.", controller);
        }

        private static AnimatorController FindController()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AnimatorController Beholder"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("/Beholder.controller"))
                    return AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            }
            return null;
        }

        private static AnimatorState FindState(AnimatorStateMachine machine, string name)
        {
            foreach (ChildAnimatorState child in machine.states)
                if (child.state.name == name) return child.state;
            return null;
        }

        private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            foreach (AnimatorControllerParameter p in controller.parameters)
            {
                if (p.name != name) continue;
                if (p.type == type) return;
                controller.RemoveParameter(p);
                break;
            }
            controller.AddParameter(name, type);
        }

        private static void SetupBlend(AnimatorStateTransition t) { t.hasExitTime = false; t.duration = 0.1f; }

        private static AnimatorStateTransition AddAny(AnimatorStateMachine machine, AnimatorState target)
        {
            AnimatorStateTransition t = machine.AddAnyStateTransition(target);
            t.hasExitTime = false;
            t.duration = 0.1f;
            t.canTransitionToSelf = true;
            return t;
        }

        private static void AddReturn(AnimatorState from, AnimatorState idle)
        {
            AnimatorStateTransition t = from.AddTransition(idle);
            t.hasExitTime = true;
            t.exitTime = 0.95f;
            t.duration = 0.15f;
        }
    }
}
#endif
