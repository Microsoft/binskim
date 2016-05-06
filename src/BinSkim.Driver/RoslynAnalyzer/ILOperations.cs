﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Semantics;
using System.Collections.Immutable;
using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.IL
{
    internal abstract class Operation : IOperation
    {
        public abstract OperationKind Kind { get; }
        public virtual ITypeSymbol Type => null;
        public virtual Optional<object> ConstantValue => default(Optional<object>);
        public virtual bool IsInvalid => false;


        public abstract void Accept(OperationVisitor visitor);
        public abstract TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument);

        public override string ToString()
        {
            var name = this.GetType().Name;

            if (name.EndsWith("Expression", StringComparison.Ordinal))
            {
                name = name.Substring(0, name.Length - "Expression".Length);
            }
            else if (name.EndsWith("Statement", StringComparison.Ordinal))
            {
                name = name.Substring(0, name.Length - "Statement".Length);
            }

            return name;
        }

        // TODO: Hang a location from PDB off of this.
        // FEEDBACK: Should this be optional, with separate location?
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        SyntaxNode IOperation.Syntax => s_fakeStatement;
        private static readonly SyntaxNode s_fakeStatement = CSharp.SyntaxFactory.EmptyStatement();
    }

    internal abstract class Expression : Operation, IOperation
    {
        public abstract override ITypeSymbol Type { get; }
    }

    internal abstract class HasArgumentsExpression : Expression, IHasArgumentsExpression
    {
        protected HasArgumentsExpression(ImmutableArray<IArgument> arguments)
        {
            Arguments = arguments;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ImmutableArray<IArgument> Arguments { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        ImmutableArray<IArgument> IHasArgumentsExpression.ArgumentsInParameterOrder => Arguments;

        public IArgument GetArgumentMatchingParameter(IParameterSymbol parameter)
        {
            int ordinal = parameter.Ordinal;
            if (ordinal < 0 || ordinal >= Arguments.Length)
            {
                return null;
            }

            var argument = Arguments[ordinal];
            if (!argument.Parameter.Equals(parameter))
            {
                return null;
            }

            return argument;
        }
    }

    internal sealed class InvocationExpression : HasArgumentsExpression, IInvocationExpression
    {
        public InvocationExpression(bool isVirtual, IOperation instance, IMethodSymbol targetMethod, ImmutableArray<IArgument> arguments)
            : base(arguments)
        {
            IsVirtual = isVirtual;
            Instance = instance;
            TargetMethod = targetMethod;
        }

        public bool IsVirtual { get; }
        public IOperation Instance { get; }
        public IMethodSymbol TargetMethod { get; }
        public override ITypeSymbol Type => TargetMethod.ReturnType;
        public override OperationKind Kind => OperationKind.InvocationExpression;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        ImmutableArray<IArgument> IInvocationExpression.ArgumentsInSourceOrder => Arguments;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInvocationExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInvocationExpression(this, argument);
        }
    }

    internal sealed class ObjectCreationExpression : HasArgumentsExpression, IObjectCreationExpression
    {
        public ObjectCreationExpression(IMethodSymbol constructor, ImmutableArray<IArgument> arguments)
            : base(arguments)
        {
            Constructor = constructor;
        }

        public IMethodSymbol Constructor { get; }

        //NOTE: Not just .ContainingType to allow for magic array methods that belong to array types.
        public override ITypeSymbol Type => Constructor.ContainingSymbol as ITypeSymbol; 
        public override OperationKind Kind => OperationKind.ObjectCreationExpression;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        ImmutableArray<ISymbolInitializer> IObjectCreationExpression.MemberInitializers => ImmutableArray<ISymbolInitializer>.Empty;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitObjectCreationExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitObjectCreationExpression(this, argument);
        }
    }

    internal sealed class Argument : Operation, IArgument
    {
        public Argument(IParameterSymbol parameter, IOperation value)
        {
            Parameter = parameter;
            Value = value;
        }

        public IParameterSymbol Parameter { get; }
        public IOperation Value { get; }
        public override OperationKind Kind => OperationKind.Argument;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        ArgumentKind IArgument.ArgumentKind => ArgumentKind.Positional;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        IOperation IArgument.InConversion => null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        IOperation IArgument.OutConversion => null;

        public override string ToString()
        {
            return $"Argument [{Parameter?.Name ?? "(vararg)"} : {Value}]";
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitArgument(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitArgument(this, argument);
        }
    }

    internal sealed class DefaultValueExpression : Expression, IDefaultValueExpression
    {
        public DefaultValueExpression(ITypeSymbol type)
        {
            Type = type;
        }

        public override ITypeSymbol Type { get; }
        public override OperationKind Kind => OperationKind.DefaultValueExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDefaultValueExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitDefaultValueExpression(this, argument);
        }
    }

    internal abstract class ReferenceExpression : Expression
    {
        protected ReferenceExpression(ITypeSymbol type)
        {
            Type = type;
        }

        public sealed override ITypeSymbol Type { get; }

        public ReferenceExpression WithType(ITypeSymbol type)
        {
            if (type == null || type == Type)
                return this;

            return WithTypeCore(type);
        }

        protected abstract ReferenceExpression WithTypeCore(ITypeSymbol type);
    }

    internal sealed class ArrayElementReferenceExpression : ReferenceExpression, IArrayElementReferenceExpression
    {
        public ArrayElementReferenceExpression(IOperation arrayReference, IOperation index, ITypeSymbol type)
            : this(arrayReference, ImmutableArray.Create(index), type)
        {
        }

        public ArrayElementReferenceExpression(IOperation arrayReference, ImmutableArray<IOperation> indices, ITypeSymbol type)
            : base(type)
        {
            ArrayReference = arrayReference;
            Indices = indices;
        }

        public IOperation ArrayReference { get; }
        public ImmutableArray<IOperation> Indices { get; }
        public override OperationKind Kind => OperationKind.ArrayElementReferenceExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitArrayElementReferenceExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitArrayElementReferenceExpression(this, argument);
        }

        protected override ReferenceExpression WithTypeCore(ITypeSymbol type)
        {
            return new ArrayElementReferenceExpression(ArrayReference, Indices, type);
        }
    }

    internal sealed class PointerIndirectionReferenceExpression : ReferenceExpression, IPointerIndirectionReferenceExpression
    {
        public PointerIndirectionReferenceExpression(IOperation pointer, ITypeSymbol type)
            : base(type)
        {
            Pointer = pointer;
        }

        public IOperation Pointer { get; }
        public override OperationKind Kind => OperationKind.PointerIndirectionReferenceExpression;

        protected override ReferenceExpression WithTypeCore(ITypeSymbol type)
        {
            return new PointerIndirectionReferenceExpression(Pointer, type);
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPointerIndirectionReferenceExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPointerIndirectionReferenceExpression(this, argument);
        }
    }

    internal class LocalReferenceExpression : ReferenceExpression, ILocalReferenceExpression
    {
        public LocalReferenceExpression(ILocalSymbol local)
            : this(local, local.Type)
        {
        }

        public LocalReferenceExpression(ILocalSymbol local, ITypeSymbol type)
            : base(type)
        {
            Local = local;
        }

        public ILocalSymbol Local { get; }
        public override OperationKind Kind => OperationKind.LocalReferenceExpression;

        public override string ToString()
        {
            return $"LocalReference [{Local.Name}]";
        }

        protected override ReferenceExpression WithTypeCore(ITypeSymbol type)
        {
            return new LocalReferenceExpression(Local, type);
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLocalReferenceExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLocalReferenceExpression(this, argument);
        }
    }

    internal class ParameterReferenceExpression : ReferenceExpression, IParameterReferenceExpression
    {
        public ParameterReferenceExpression(IParameterSymbol parameter)
            : this(parameter, parameter.Type)
        {
        }

        public ParameterReferenceExpression(IParameterSymbol parameter, ITypeSymbol type)
            : base(type)
        {
            Parameter = parameter;
        }

        public IParameterSymbol Parameter { get; }
        public override OperationKind Kind => OperationKind.ParameterReferenceExpression;

        public override string ToString()
        {
            return $"ParameterReference [{Parameter.Name}]";
        }

        protected override ReferenceExpression WithTypeCore(ITypeSymbol type)
        {
            return new ParameterReferenceExpression(Parameter, type);
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitParameterReferenceExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitParameterReferenceExpression(this, argument);
        }
    }

    internal sealed class InstanceReferenceExpression : ReferenceExpression, IInstanceReferenceExpression
    {
        public InstanceReferenceExpression(IMethodSymbol method)
            : this(method.ContainingType)
        {
        }

        public InstanceReferenceExpression(ITypeSymbol type)
            : base(type)
        {
        }

        public InstanceReferenceKind InstanceReferenceKind => InstanceReferenceKind.Explicit; // TODO/FEEDBACK: needs to be adjusted to base sometimes, but what about other bindings?
        public override OperationKind Kind => OperationKind.InstanceReferenceExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInstanceReferenceExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInstanceReferenceExpression(this, argument);
        }

        public override string ToString()
        {
            return "InstanceReference";
        }

        protected override ReferenceExpression WithTypeCore(ITypeSymbol type)
        {
            return new InstanceReferenceExpression(type);
        }
    }

    internal abstract class MemberReferenceExpression : ReferenceExpression, IMemberReferenceExpression
    {
        protected MemberReferenceExpression(IOperation instance, ISymbol member, ITypeSymbol type)
            : base(type)
        {
            Instance = instance;
            Member = member;
        }

        public IOperation Instance { get; }
        public ISymbol Member { get; }
    }

    internal sealed class FieldReferenceExpression : MemberReferenceExpression, IFieldReferenceExpression
    {
        public FieldReferenceExpression(IOperation instance, IFieldSymbol field)
            : this(instance, field, field.Type)
        {
        }

        public FieldReferenceExpression(IOperation instance, ISymbol member, ITypeSymbol type)
            : base(instance, member, type)
        {
        }

        public IFieldSymbol Field => (IFieldSymbol)Member;
        public override OperationKind Kind => OperationKind.FieldReferenceExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitFieldReferenceExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitFieldReferenceExpression(this, argument);
        }

        protected override ReferenceExpression WithTypeCore(ITypeSymbol type)
        {
            return new FieldReferenceExpression(Instance, Field, type);
        }
    }

    internal abstract class HasOperatorMethodExpression : Expression, IHasOperatorMethodExpression
    {
        // operator method calls will be raised as regular invocations so this is always false/null.
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool IHasOperatorMethodExpression.UsesOperatorMethod => false;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        IMethodSymbol IHasOperatorMethodExpression.OperatorMethod => null;
    }

    internal sealed class UnaryOperatorExpression : HasOperatorMethodExpression, IUnaryOperatorExpression
    {
        public UnaryOperatorExpression(UnaryOperationKind unaryOperationKind, IOperation operand, ITypeSymbol type)
        {
            UnaryOperationKind = unaryOperationKind;
            Operand = operand;
            Type = type;
        }

        public UnaryOperationKind UnaryOperationKind { get; }
        public IOperation Operand { get; }
        public override ITypeSymbol Type { get; }

        public override OperationKind Kind => OperationKind.UnaryOperatorExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitUnaryOperatorExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitUnaryOperatorExpression(this, argument);
        }
    }

    internal sealed class BinaryOperatorExpression : HasOperatorMethodExpression, IBinaryOperatorExpression
    {
        public BinaryOperatorExpression(BinaryOperationKind binaryOperationKind, IOperation left, IOperation right, ITypeSymbol type)
        {
            BinaryOperationKind = binaryOperationKind;
            LeftOperand = left;
            RightOperand = right;
            Type = type;
        }

        public BinaryOperationKind BinaryOperationKind { get; }
        public IOperation LeftOperand { get; }
        public IOperation RightOperand { get; }
        public override ITypeSymbol Type { get; }
        public override OperationKind Kind => OperationKind.BinaryOperatorExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitBinaryOperatorExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitBinaryOperatorExpression(this, argument);
        }
    }

    internal sealed class ConversionExpression : HasOperatorMethodExpression, IConversionExpression
    {
        public ConversionExpression(ConversionKind conversionKind, IOperation operand, ITypeSymbol type)
        {
            ConversionKind = conversionKind;
            Operand = operand;
            Type = type;
        }

        public ConversionKind ConversionKind { get; }
        public IOperation Operand { get; }
        public override ITypeSymbol Type { get; }
        public override OperationKind Kind => OperationKind.ConversionExpression;

        bool IConversionExpression.IsExplicit => true;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitConversionExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitConversionExpression(this, argument);
        }
    }

    internal sealed class SizeOfExpression : Expression, ISizeOfExpression
    {
        public SizeOfExpression(Compilation compilation, ITypeSymbol typeOperand)
        {
            TypeOperand = typeOperand;
            Type = compilation.GetSpecialType(SpecialType.System_UInt32);
        }

        public ITypeSymbol TypeOperand { get; }
        public override ITypeSymbol Type { get; }
        public override OperationKind Kind => OperationKind.SizeOfExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitSizeOfExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitSizeOfExpression(this, argument);
        }
    }

    internal sealed class LiteralExpression : Expression, ILiteralExpression
    {
        public LiteralExpression(object value, ITypeSymbol type)
        {
            ConstantValue = value;
            Type = type;
        }

        public override Optional<object> ConstantValue { get; }
        public override ITypeSymbol Type { get; }
        public override OperationKind Kind => OperationKind.LiteralExpression;

        // TODO: Proper IL/round-trippable syntax.
        public string Text => ConstantValue.ToString();

        public override string ToString()
        {
            return $"Literal [{ConstantValue.Value ?? "null"}]";
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLiteralExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLiteralExpression(this, argument);
        }
    }

    internal sealed class AddressOfExpression : Expression, IAddressOfExpression
    {
        public AddressOfExpression(Compilation compilation, IOperation reference)
        {
            Reference = reference;
            Type = compilation.CreatePointerTypeSymbol(reference.Type);
        }

        public IOperation Reference { get; }
        public override ITypeSymbol Type { get; }
        public override OperationKind Kind => OperationKind.AddressOfExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitAddressOfExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitAddressOfExpression(this, argument);
        }
    }

    internal sealed class ArrayCreationExpression : Expression, IArrayCreationExpression
    {
        public ArrayCreationExpression(Compilation compilation, ITypeSymbol elementType, IOperation size)
        {
            ElementType = elementType;
            DimensionSizes = ImmutableArray.Create(size);
            Type = compilation.CreateArrayTypeSymbol(elementType);
        }

        public ITypeSymbol ElementType { get; }
        public ImmutableArray<IOperation> DimensionSizes { get; }
        public override ITypeSymbol Type { get; }
        public override OperationKind Kind => OperationKind.ArrayCreationExpression;

        IArrayInitializer IArrayCreationExpression.Initializer => null;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitArrayCreationExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitArrayCreationExpression(this, argument);
        }
    }

    internal sealed class AssignmentExpression : Expression, IAssignmentExpression
    {
        public AssignmentExpression(IOperation target, IOperation value)
        {
            Target = target;
            Value = value;
        }

        public IOperation Target { get; }
        public IOperation Value { get; }

        public override OperationKind Kind => OperationKind.AssignmentExpression;
        public override ITypeSymbol Type => Target.Type;

        public override string ToString()
        {
            return $"Assignment [{Target} = {Value}]";
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitAssignmentExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitAssignmentExpression(this, argument);
        }
    }

    internal class BlockStatement : Operation, IBlockStatement
    {
        public BlockStatement(ImmutableArray<IOperation> statements)
            : this(statements, ImmutableArray<ILocalSymbol>.Empty)
        {
        }

        public BlockStatement(ImmutableArray<IOperation> statements, ImmutableArray<ILocalSymbol> locals)
        {
            Locals = locals;
            Statements = statements;
        }

        public ImmutableArray<ILocalSymbol> Locals { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ImmutableArray<IOperation> Statements { get; }
        public override OperationKind Kind => OperationKind.BlockStatement;

        // We allow block statements to be used as expressions (which is particularly useful for
        // arbitrary exception filters). The interpretation is that the final operation yields
        // the result.
        public override ITypeSymbol Type => Statements[Statements.Length - 1].Type;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitBlockStatement(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitBlockStatement(this, argument);
        }
    }

    internal sealed class LabelStatement : Operation, ILabelStatement
    {
        public LabelStatement(ILabelSymbol label)
        {
            Label = label;
        }

        public ILabelSymbol Label { get; }
        public override OperationKind Kind => OperationKind.LabelStatement;

        // This is allowed to be null and so we do that uniformly.
        IOperation ILabelStatement.LabeledStatement => null;

        public override string ToString()
        {
            return $"Label: {Label.Name}";
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLabelStatement(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLabelStatement(this, argument);
        }
    }

    internal sealed class IfStatement : Operation, IIfStatement
    {
        public IfStatement(IOperation condition, IOperation ifTrue)
        {
            Condition = condition;
            IfTrueStatement = ifTrue;
        }
        public IOperation Condition { get; }
        public IOperation IfTrueStatement { get; }
        public override OperationKind Kind => OperationKind.IfStatement;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        IOperation IIfStatement.IfFalseStatement => null; // we always goto on true and fallthrough on false 

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitIfStatement(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitIfStatement(this, argument);
        }
    }

    internal sealed class BranchStatement : Operation, IBranchStatement
    {
        public BranchStatement(ILabelSymbol target)
        {
            Target = target;
        }

        public ILabelSymbol Target { get; }
        public override OperationKind Kind => OperationKind.BranchStatement;
        public BranchKind BranchKind => BranchKind.GoTo;

        public override string ToString()
        {
            return $"GoTo {Target.Name}";
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitBranchStatement(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitBranchStatement(this, argument);
        }
    }

    internal sealed class ThrowStatement : Operation, IThrowStatement
    {
        public ThrowStatement(IOperation thrown)
        {
            ThrownObject = thrown;
        }

        public IOperation ThrownObject { get; }
        public override OperationKind Kind => OperationKind.ThrowStatement;

        public override string ToString()
        {
            return ThrownObject == null ? "Throw" : $"Throw [{ThrownObject}]";
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitThrowStatement(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitThrowStatement(this, argument);
        }
    }

    internal sealed class ReturnStatement : Operation, IReturnStatement
    {
        public ReturnStatement(IOperation returned)
        {
            ReturnedValue = returned;
        }

        public IOperation ReturnedValue { get; }
        public override OperationKind Kind => OperationKind.ReturnStatement;

        public override string ToString()
        {
            return ReturnedValue == null ? "Return" : $"Return [{ReturnedValue}]";
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitReturnStatement(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitReturnStatement(this, argument);
        }
    }

    internal abstract class TryStatement : Operation, ITryStatement
    {
        public TryStatement(IBlockStatement body)
        {
            Body = body;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public IBlockStatement Body { get; }

        public virtual ImmutableArray<ICatchClause> Catches => ImmutableArray<ICatchClause>.Empty;
        public virtual IBlockStatement FinallyHandler => null;

        public override OperationKind Kind => OperationKind.TryStatement;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitTryStatement(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitTryStatement(this, argument);
        }
    }

    internal sealed class TryCatchStatement : TryStatement
    {
        public TryCatchStatement(IBlockStatement body, ImmutableArray<ICatchClause> catches)
            : base(body)
        {
            Catches = catches;
        }

        public override ImmutableArray<ICatchClause> Catches { get; }
    }

    internal sealed class TryFinallyStatement : TryStatement
    {
        public TryFinallyStatement(IBlockStatement body, IBlockStatement finallyHandler)
            : base(body)
        {
            FinallyHandler = finallyHandler;
        }

        public override IBlockStatement FinallyHandler { get; }
    }

    internal sealed class CatchClause : Operation, ICatchClause
    {
        public CatchClause(ITypeSymbol caughtType, ILocalSymbol exceptionLocal, IOperation filter, IBlockStatement handler)
        {
            CaughtType = caughtType;
            ExceptionLocal = exceptionLocal;
            Filter = filter;
            Handler = handler;
        }

        public ITypeSymbol CaughtType { get; }
        public ILocalSymbol ExceptionLocal { get; }
        public IOperation Filter { get; }
        public IBlockStatement Handler { get; }

        public override OperationKind Kind => OperationKind.CatchClause;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitCatch(this); // FEEDBACK: inconsistent naming
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitCatch(this, argument); // FEEDBACK: inconsistent naming
        }
    }

    internal sealed class SwitchStatement : Operation, ISwitchStatement
    {
        public SwitchStatement(IOperation value, ImmutableArray<ISwitchCase> cases)
        {
            Value = value;
            Cases = cases;
        }

        public IOperation Value { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ImmutableArray<ISwitchCase> Cases { get; }

        public override OperationKind Kind => OperationKind.SwitchStatement;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitSwitchStatement(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitSwitchStatement(this, argument);
        }
    }

    internal sealed class SwitchCase : Operation, ISwitchCase
    {
        public SwitchCase(IOperation value, IOperation body)
        {
            Clauses = ImmutableArray.Create<ICaseClause>(new Clause(value));
            Body = ImmutableArray.Create(body);
        }

        public ImmutableArray<ICaseClause> Clauses { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ImmutableArray<IOperation> Body { get; }
        
        public override OperationKind Kind => OperationKind.SwitchCase;

        public override string ToString()
        {
            return $"Case {((Clause)Clauses[0]).Value}";
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitSwitchCase(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitSwitchCase(this, argument);
        }

        private sealed class Clause : Operation, ISingleValueCaseClause
        {
            public Clause(IOperation value)
            {
                Value = value;
            }

            public IOperation Value { get; }

            public BinaryOperationKind Equality => BinaryOperationKind.IntegerEquals;
            public CaseKind CaseKind => CaseKind.SingleValue;
            public override OperationKind Kind => OperationKind.SingleValueCaseClause;

            public override void Accept(OperationVisitor visitor)
            {
                visitor.VisitSingleValueCaseClause(this);
            }

            public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            {
                return visitor.VisitSingleValueCaseClause(this, argument);
            }
        }
    }

    internal sealed class InvalidStatement : Operation, IInvalidStatement
    {
        public InvalidStatement(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; }
        public override bool IsInvalid => true;
        public override OperationKind Kind => OperationKind.InvalidStatement;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInvalidStatement(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInvalidStatement(this, argument);
        }
    }
}
