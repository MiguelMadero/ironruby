/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Microsoft Public License, please send an email to 
 * ironruby@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Microsoft Public License.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using IronRuby.Builtins;

namespace IronRuby.Runtime.Calls {

    public class RubyMemberInfo {
        // Singleton used to undefine methods: stops method resolution
        internal static readonly RubyMemberInfo/*!*/ UndefinedMethod = new RubyMemberInfo();

        // Singleton used to hide CLR methods: method resolution skips all CLR methods since encountering a hidden method.
        internal static readonly RubyMemberInfo/*!*/ HiddenMethod = new RubyMemberInfo();

        // Singleton used to represent foreign members (these are not in method tables):
        internal static readonly RubyMemberInfo/*!*/ ForeignMember = new RubyMemberInfo();

        private readonly RubyMemberFlags _flags;

        // Null for dummy methods.
        private readonly RubyModule _declaringModule;
        
        #region Mutable state guarded by ClassHierarchyLock

        private bool _invalidateSitesOnOverride;
        private bool _invalidateGroupsOnRemoval;

        #endregion

        public RubyMethodVisibility Visibility {
            get { return (RubyMethodVisibility)(_flags & RubyMemberFlags.VisibilityMask); }
        }

        public bool IsProtected {
            get { return (_flags & RubyMemberFlags.Protected) != 0; }
        }

        public bool IsPrivate {
            get { return (_flags & RubyMemberFlags.Private) != 0; }
        }

        public bool IsPublic {
            get { return (_flags & RubyMemberFlags.Public) != 0; }
        }
        
        internal bool IsModuleFunction {
            get { return (_flags & RubyMemberFlags.ModuleFunction) != 0; }
        }

        internal bool IsEmpty {
            get { return (_flags & RubyMemberFlags.Empty) != 0; }
        }

        internal bool IsSuperForwarder {
            get { return (_flags & RubyMemberFlags.SuperForwarder) != 0; }
        }

        /// <summary>
        /// Whether the member can be permanently removed (CLR members can't).
        /// If the member cannot be removed we hide it.
        /// </summary>
        internal virtual bool IsRemovable {
            get { return IsSuperForwarder; }
        }

        internal RubyMemberFlags Flags {
            get { return _flags; }
        }

        /// <summary>
        /// True if this member hides any CLR overloads of groups below it.
        /// </summary>
        /// <remarks>
        /// Undefined and Hidden method singletons cannot be removed so they don't need to be marked.
        /// </remarks>
        internal bool InvalidateGroupsOnRemoval {
            get {
                // RequiresClassHierarchyLock
                return _invalidateGroupsOnRemoval;
            }
            set {
                // RequiresClassHierarchyLock
                Debug.Assert(IsRemovable);
                _invalidateGroupsOnRemoval = value;
            }
        }

        /// <summary>
        /// Method definition that replaces/overrides this method will cause version update of all dependent subclasses/modules, which
        /// triggers invalidation of sites that are bound to those classes.
        /// </summary>
        internal bool InvalidateSitesOnOverride {
            get {
                Context.RequiresClassHierarchyLock();
                return _invalidateSitesOnOverride;
            }
        }

        internal virtual void SetInvalidateSitesOnOverride() {
            _invalidateSitesOnOverride = true;
        }

        internal static void SetInvalidateSitesOnOverride(RubyMemberInfo/*!*/ member) {
            member._invalidateSitesOnOverride = true;
        }

        public RubyModule/*!*/ DeclaringModule {
            get {
                Debug.Assert(_declaringModule != null);
                return _declaringModule; 
            }
        }

        public RubyContext/*!*/ Context {
            get {
                Debug.Assert(_declaringModule != null);
                return _declaringModule.Context;
            }
        }

        // TODO: 
        public virtual int GetArity() {
            return 0;
        }

        public bool IsUndefined {
            get { return ReferenceEquals(this, UndefinedMethod); }
        }

        public bool IsHidden {
            get { return ReferenceEquals(this, HiddenMethod); }
        }

        // undefined, hidden method:
        private RubyMemberInfo() {
        }

        internal RubyMemberInfo(RubyMemberFlags flags, RubyModule/*!*/ declaringModule) {
            Assert.NotNull(declaringModule);
            Debug.Assert(flags != RubyMemberFlags.Invalid);

            _flags = flags;
            _declaringModule = declaringModule;
        }

        internal protected virtual RubyMemberInfo Copy(RubyMemberFlags flags, RubyModule/*!*/ module) {
            throw Assert.Unreachable;
        }

        public override string/*!*/ ToString() {
            return 
                IsHidden ? "<hidden>" :
                IsUndefined ? "<undefined>" :
                (GetType().Name + " " + _flags.ToString() + " (" + _declaringModule.Name + ")");
        }

        /// <summary>
        /// Gets all the CLR members represented by this member info. 
        /// </summary>
        public virtual MemberInfo/*!*/[]/*!*/ GetMembers() {
            throw Assert.Unreachable;
        }

        /// <summary>
        /// Returns a copy of this member info that groups only those members of this member info that are generic
        /// and of generic arity equal to the length of the given array of type arguments. Returns null if there are no such generic members.
        /// All the members in the resulting info are constructed generic methods bound to the given type arguments.
        /// </summary>
        public virtual RubyMemberInfo TryBindGenericParameters(Type/*!*/[]/*!*/ typeArguments) {
            return null;
        }

        public virtual RubyMemberInfo TrySelectOverload(Type/*!*/[]/*!*/ parameterTypes) {
            throw Assert.Unreachable;
        }

        #region Dynamic Operations

        internal virtual void BuildCallNoFlow(MetaObjectBuilder/*!*/ metaBuilder, CallArguments/*!*/ args, string/*!*/ name) {
            throw Assert.Unreachable;
        }

        internal void BuildCall(MetaObjectBuilder/*!*/ metaBuilder, CallArguments/*!*/ args, string/*!*/ name) {
            BuildCallNoFlow(metaBuilder, args, name);
            metaBuilder.BuildControlFlow(args);
        }

        internal virtual void BuildSuperCallNoFlow(MetaObjectBuilder/*!*/ metaBuilder, CallArguments/*!*/ args, string/*!*/ name, RubyModule/*!*/ declaringModule) {
            BuildCallNoFlow(metaBuilder, args, name);
        }

        internal void BuildSuperCall(MetaObjectBuilder/*!*/ metaBuilder, CallArguments/*!*/ args, string/*!*/ name, RubyModule/*!*/ declaringModule) {
            BuildSuperCallNoFlow(metaBuilder, args, name, declaringModule);
            metaBuilder.BuildControlFlow(args);
        }

        #endregion
    }
}
