﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Deveel.Workflows.Graph;

namespace Deveel.Workflows {
	class ActivityBuilder : IActivityBuilder, INamedActivityBuilder, IConditionalActivityBuilder, IExecutionNodeBuilder {
		private string Name { get; set; }

		private Dictionary<string, object> Metadata { get; set; }

		private Func<State, bool> Decision { get; set; }

		private Type ActivityType { get; set; }

		private BranchBuilder BranchBuilder { get; set; }

		private IActivity ProxyActivity { get; set; }

		private Func<State, CancellationToken, Task<State>> Execution { get; set; }

		private bool OptionSet { get; set; }

		private void AssertOptionNotSet() {
			if (OptionSet)
				throw new InvalidOperationException("An activity build option has already been set");
		}

		public INamedActivityBuilder Named(string name) {
			Name = name;
			return this;
		}

		public INamedActivityBuilder With(string key, object metadata) {
			if (Metadata == null)
				Metadata = new Dictionary<string, object>();

			Metadata[key] = metadata;
			return this;
		}

		public IActivity Build(IBuildContext context) {
			if (!OptionSet)
				throw new InvalidOperationException();

			IActivity activity;

			if (ActivityType != null) {
				if (context == null)
					throw new NotSupportedException("The builder references a type that cannot be resolved outside a context");

				activity = context.ResolveActivity(ActivityType);
			} else if (ProxyActivity != null) {
				activity = ProxyActivity;
			} else if (BranchBuilder != null) {
				activity = BranchBuilder.Build(context);
			} else {
				activity = new Activity(Name, Decision, Execution);

				if (Metadata != null) {
					((Activity)activity).SetMetadata(Metadata);
				}
			}

			return activity;
		}

		public ExecutionNode BuildNode() {
			if (!OptionSet)
				throw new InvalidOperationException();

			ExecutionNode node;

			if (ActivityType != null) {
				throw new NotImplementedException();
			} else if (ProxyActivity != null) {
				node = new ComponentNode(ProxyActivity);
			} else if (BranchBuilder != null) {
				node = BranchBuilder.BuildNode();
			} else {
				node = new BuilderNode(Name, Decision != null, Metadata);
			}

			return node;
		}

		public IConditionalActivityBuilder If(Func<State, bool> decision) {
			Decision = decision;
			return this;
		}

		public void Branch(Action<IBranchBuilder> branch) {
			AssertOptionNotSet();

			var builder = new BranchBuilder();
			branch(builder);

			BranchBuilder = builder;
			OptionSet = true;
		}

		public void OfType(Type type) {
			AssertOptionNotSet();

			ActivityType = type;
			OptionSet = true;
		}

		public void Proxy(IActivity activity) {
			AssertOptionNotSet();

			ProxyActivity = activity;
			OptionSet = true;
		}

		public void Execute(Func<State, CancellationToken, Task<State>> execution) {
			AssertOptionNotSet();

			Execution = execution;
			OptionSet = true;
		}

		public IExecutionNode BuildGraphNode() {
			throw new NotImplementedException();
		}
	}
}