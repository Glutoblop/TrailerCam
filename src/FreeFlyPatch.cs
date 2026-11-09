using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace TrailerCam
{
    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateFreeFly))]
    internal static class FreeFlyPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            const BindingFlags staticHelper = BindingFlags.Static | BindingFlags.NonPublic;
            var buttonDown = typeof(ZInput).GetMethod(nameof(ZInput.GetButtonDown), new[] { typeof(string) });
            var lockButtonDown = typeof(FreeFlyPatch).GetMethod(nameof(LockButtonDown), staticHelper);
            var velocityDamp = typeof(Vector3).GetMethod(nameof(Vector3.SmoothDamp),
                new[] { typeof(Vector3), typeof(Vector3), typeof(Vector3).MakeByRefType(), typeof(float), typeof(float), typeof(float) });
            var smoothField = typeof(GameCamera).GetField(nameof(GameCamera.m_freeFlySmooth),
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var movementSmoothing = typeof(Plugin).GetMethod(nameof(Plugin.GetMovementSmoothing), staticHelper);

            // The velocity block reads the field for both its zero check and its damp time.
            // The later reads belong to rotation and keep the game's existing path.
            int velocityDampIndex = code.FindIndex(instruction => instruction.Calls(velocityDamp));
            var movementReads = code.Take(velocityDampIndex).Where(instruction => instruction.LoadsField(smoothField)).ToList();
            if (movementReads.Count == 2)
            {
                foreach (var read in movementReads)
                {
                    read.opcode = OpCodes.Call;
                    read.operand = movementSmoothing;
                }
            }
            else
            {
                Debug.LogWarning("[Trailer Cam] Could not find the free-camera movement smoothing instructions.");
            }

            for (int i = 0; i < code.Count; i++)
            {
                var instruction = code[i];
                if (i > 0 && instruction.Calls(buttonDown) && code[i - 1].opcode == OpCodes.Ldstr
                    && code[i - 1].operand is "Attack" or "SecondaryAttack" or "JoyLStick" or "JoyRStick")
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction).MoveBlocksFrom(instruction);
                    instruction.operand = lockButtonDown;
                }
                yield return instruction;
            }
        }

        private static bool LockButtonDown(string button, GameCamera camera) =>
            !Plugin.IsFilming(camera) && ZInput.GetButtonDown(button);
    }
}
