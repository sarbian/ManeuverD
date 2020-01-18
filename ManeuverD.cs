using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;

namespace ManeuverD
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class ManeuverD : MonoBehaviour
    {
        private void Start()
        {
            Log("Starting");
            
            if (Versioning.version_major >= 1 && Versioning.version_minor >= 8 && Versioning.Revision > 1)
            {
                Log("Version of KSP after 1.8.1 are not supported yet");
                return;
            }

            HarmonyInstance harmony = HarmonyInstance.Create("com.sarbian.ManeuverD");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void Log(object message)
        {
            Debug.Log("[ManeuverD] " + message);
        }
    }

    [HarmonyPatch(typeof(PatchedConicSolver))]
    [HarmonyPatch("CheckNextManeuver")]
    public static class PatchedConicSolver_CheckNextManeuver_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {

            ManeuverD.Log("Patching PatchedConicSolver.CheckNextManeuver");

            int state = 0;

            foreach (CodeInstruction instruction in instructions)
            {
                //ManeuverD.Log("state=" + state + " " + instruction.opcode + " \"" + instruction.operand as string + "\"");
                
                // We remove the first Vector3d to Vector3 cast
                if (state == 0 && instruction.opcode == OpCodes.Call && instruction.operand.ToString() == "UnityEngine.Vector3 op_Implicit(Vector3d)" )
                {
                    state++;
                    continue;
                }

                // We remove the second Vector3d to Vector3 cast
                if (state == 1 && instruction.opcode == OpCodes.Call && instruction.operand.ToString()   == "UnityEngine.Vector3 op_Implicit(Vector3d)" )
                {
                    state++;
                    continue;
                }
                
                // We replace Quaternion.LookRotation with our QuaternionD.LookRotation
                if (state == 2 && instruction.opcode == OpCodes.Call && instruction.operand.ToString()  == "UnityEngine.Quaternion LookRotation(UnityEngine.Vector3, UnityEngine.Vector3)" )
                {
                    state++;
                    CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => LookRotation(Vector3d.zero, Vector3d.zero)));
                    //ManeuverD.Log("N state=" + state + " " + codeInstruction.opcode + " \"" + codeInstruction.operand as string + "\"");
                    yield return codeInstruction;
                    continue;
                }
                
                // We remove the Quaternion to QuaternionD cast
                if (state == 3 && instruction.opcode == OpCodes.Call && instruction.operand.ToString() == "UnityEngine.QuaternionD op_Implicit(UnityEngine.Quaternion)" )
                {
                    state++;
                    continue;
                }

                //ManeuverD.Log("state=" + state + " " + instruction.opcode + " \"" + instruction.operand as string + "\"");

                yield return instruction;
            }
        }

        // Double precision LookRotation.
        public static QuaternionD LookRotation(Vector3d forward, Vector3d up)
        {
            forward = Vector3d.Normalize(forward);
            Vector3d right = Vector3d.Normalize(Vector3d.Cross(up, forward));
            up = Vector3d.Cross(forward, right);
            double m00 = right.x;
            double m01 = right.y;
            double m02 = right.z;
            double m10 = up.x;
            double m11 = up.y;
            double m12 = up.z;
            double m20 = forward.x;
            double m21 = forward.y;
            double m22 = forward.z;
            
            double num8 = (m00 + m11) + m22;
            QuaternionD quaternion = new QuaternionD();
            if (num8 > 0f)
            {
                double num = Math.Sqrt(num8 + 1);
                quaternion.w = num * 0.5;
                num = 0.5 / num;
                quaternion.x = (m12 - m21) * num;
                quaternion.y = (m20 - m02) * num;
                quaternion.z = (m01 - m10) * num;
                return quaternion;
            }

            if ((m00 >= m11) && (m00 >= m22))
            {
                double num7 = Math.Sqrt(((1 + m00) - m11) - m22);
                double num4 = 0.5 / num7;
                quaternion.x = 0.5 * num7;
                quaternion.y = (m01 + m10) * num4;
                quaternion.z = (m02 + m20) * num4;
                quaternion.w = (m12 - m21) * num4;
                return quaternion;
            }

            if (m11 > m22)
            {
                double num6 = Math.Sqrt(((1 + m11) - m00) - m22);
                double num3 = 0.5 / num6;
                quaternion.x = (m10 + m01) * num3;
                quaternion.y = 0.5 * num6;
                quaternion.z = (m21 + m12) * num3;
                quaternion.w = (m20 - m02) * num3;
                return quaternion;
            }

            double num5 = Math.Sqrt(((1 + m22) - m00) - m11);
            double num2 = 0.5 / num5;
            quaternion.x = (m20 + m02) * num2;
            quaternion.y = (m21 + m12) * num2;
            quaternion.z = 0.5 * num5;
            quaternion.w = (m01 - m10) * num2;
            return quaternion;
        }
    }

    [HarmonyPatch(typeof(ManeuverNode))]
    [HarmonyPatch("GetPartialDv")]
    public static class ManeuverNode_GetPartialDv_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {

            ManeuverD.Log("Patching ManeuverNode.GetPartialDv");

            int state = 0;

            foreach (var instruction in instructions)
            {
                // We remove the first Vector3d to Vector3 cast
                if (state == 0 && instruction.opcode == OpCodes.Call &&
                    instruction.operand.ToString() == "UnityEngine.Vector3 op_Implicit(Vector3d)")
                {
                    state++;
                    continue;
                }

                // We remove the second Vector3d to Vector3 cast
                if (state == 1 && instruction.opcode == OpCodes.Call &&
                    instruction.operand.ToString() == "UnityEngine.Vector3 op_Implicit(Vector3d)")
                {
                    state++;
                    continue;
                }

                // We replace Quaternion.LookRotation with QuaternionD.LookRotation
                if (state == 2 && instruction.opcode == OpCodes.Call && instruction.operand.ToString() ==
                    "UnityEngine.Quaternion LookRotation(UnityEngine.Vector3, UnityEngine.Vector3)")
                {
                    state++;
                    yield return new CodeInstruction(OpCodes.Call,
                        SymbolExtensions.GetMethodInfo(() => PatchedConicSolver_CheckNextManeuver_Patch.LookRotation(Vector3d.zero, Vector3d.zero)));
                    continue;
                    //yield return new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => QuaternionD.LookRotation(Vector3d.zero, Vector3d.zero)));
                }

                // We remove the Quaternion to QuaternionD cast
                if (state == 3 && instruction.opcode == OpCodes.Call && instruction.operand.ToString() ==
                    "UnityEngine.QuaternionD op_Implicit(UnityEngine.Quaternion)")
                {
                    state++;
                    continue;
                }

                yield return instruction;
            }
        }
    }
}
