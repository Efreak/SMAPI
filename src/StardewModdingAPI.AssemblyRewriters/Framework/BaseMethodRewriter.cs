using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace StardewModdingAPI.AssemblyRewriters.Framework
{
    /// <summary>Base class for a method rewriter.</summary>
    public abstract class BaseMethodRewriter : IInstructionRewriter
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Get whether a CIL instruction should be rewritten.</summary>
        /// <param name="instruction">The IL instruction.</param>
        /// <param name="platformChanged">Whether the mod was compiled on a different platform.</param>
        public bool ShouldRewrite(Instruction instruction, bool platformChanged)
        {
            if (instruction.OpCode != OpCodes.Call && instruction.OpCode != OpCodes.Callvirt)
                return false; // not a method reference
            return this.ShouldRewrite(instruction, (MethodReference)instruction.Operand, platformChanged);
        }

        /// <summary>Rewrite a CIL instruction for compatibility.</summary>
        /// <param name="module">The module being rewritten.</param>
        /// <param name="cil">The CIL rewriter.</param>
        /// <param name="instruction">The instruction to rewrite.</param>
        /// <param name="assemblyMap">Metadata for mapping assemblies to the current platform.</param>
        public void Rewrite(ModuleDefinition module, ILProcessor cil, Instruction instruction, PlatformAssemblyMap assemblyMap)
        {
            MethodReference methodRef = (MethodReference)instruction.Operand;
            this.Rewrite(module, cil, instruction, methodRef, assemblyMap);
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Get whether a method reference should be rewritten.</summary>
        /// <param name="instruction">The IL instruction.</param>
        /// <param name="methodRef">The method reference.</param>
        /// <param name="platformChanged">Whether the mod was compiled on a different platform.</param>
        protected abstract bool ShouldRewrite(Instruction instruction, MethodReference methodRef, bool platformChanged);

        /// <summary>Rewrite a method for compatibility.</summary>
        /// <param name="module">The module being rewritten.</param>
        /// <param name="cil">The CIL rewriter.</param>
        /// <param name="instruction">The instruction which calls the method.</param>
        /// <param name="methodRef">The method reference invoked by the <paramref name="instruction"/>.</param>
        /// <param name="assemblyMap">Metadata for mapping assemblies to the current platform.</param>
        protected abstract void Rewrite(ModuleDefinition module, ILProcessor cil, Instruction instruction, MethodReference methodRef, PlatformAssemblyMap assemblyMap);

        /// <summary>Get whether a method definition matches the signature expected by a method reference.</summary>
        /// <param name="definition">The method definition.</param>
        /// <param name="reference">The method reference.</param>
        protected bool HasMatchingSignature(MethodInfo definition, MethodReference reference)
        {
            // same name
            if (definition.Name != reference.Name)
                return false;

            // same arguments
            ParameterInfo[] definitionParameters = definition.GetParameters();
            ParameterDefinition[] referenceParameters = reference.Parameters.ToArray();
            if (referenceParameters.Length != definitionParameters.Length)
                return false;
            for (int i = 0; i < referenceParameters.Length; i++)
            {
                if (!RewriteHelper.IsMatchingType(definitionParameters[i].ParameterType, referenceParameters[i].ParameterType))
                    return false;
            }
            return true;
        }

        /// <summary>Get whether a type has a method whose signature matches the one expected by a method reference.</summary>
        /// <param name="type">The type to check.</param>
        /// <param name="reference">The method reference.</param>
        protected bool HasMatchingSignature(Type type, MethodReference reference)
        {
            return type
                .GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public)
                .Any(method => this.HasMatchingSignature(method, reference));
        }
    }
}
