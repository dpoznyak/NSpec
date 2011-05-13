﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NSpec.Domain.Extensions;

namespace NSpec.Domain
{
    public class Context
    {
        public void Befores()
        {
            if (Parent != null) Parent.Befores();

            if (Before != null) Before();
        }

        public void Acts()
        {
            if (Parent != null) Parent.Acts();

            if (Act != null) Act();
        }

        public void Afters()
        {
            if (After != null) After();
        }

        public void AddExample(Example example)
        {
            example.Context = this;

            Examples.Add(example);

            example.Pending |= IsPending();
        }

        public IEnumerable<Example> AllExamples()
        {
            return Contexts.Examples().Union(Examples);
        }

        public bool IsPending()
        {
            return isPending || (Parent != null && Parent.IsPending());
        }

        public IEnumerable<Example> Failures()
        {
            return AllExamples().Where(e => e.Exception != null);
        }

        public void AddContext(Context child)
        {
            child.Parent = this;

            Contexts.Add(child);
        }

        public void Run()
        {
            Contexts.Do(c => c.Run());

            if (Method != null)
            {
                nspec instance = CreateNSpecInstance();

                try
                {
                    Method.Invoke(instance, null);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception executing context: {0}".With(FullContext()));

                    throw e;
                }
            }

            if (this is ClassContext)
            {
                //haven't figured out how to write a failing test but
                //using regular iteration causes Collection was modified
                //exception when running samples (rake samples)
                for (int i = 0; i < Examples.Count; i++)
                    CreateNSpecInstance().Exercise(Examples[i]);
            }
        }

        private nspec CreateNSpecInstance()
        {
            NSpecInstance = GetSpecType().Instance<nspec>();

            SetInstanceContext(NSpecInstance);

            NSpecInstance.Context = this;

            return NSpecInstance;
        }

        public void SetInstanceContext(nspec instance)
        {
            if (BeforeInstance != null) Before = () => BeforeInstance(instance);

            if (ActInstance != null) Act = () => ActInstance(instance);

            if (Parent != null) Parent.SetInstanceContext(instance);
        }

        private Type GetSpecType()
        {
            return Type ?? Parent.GetSpecType();
        }

        public string FullContext()
        {
            return Parent != null ? Parent.FullContext() + ". " + Name : Name;
        }

        public Context(string name = "") : this(name, 0) { }

        public Context(string name, int level) : this(name, level, false){ }

        public Context(string name, int level, bool isPending)
        {
            Name = name.Replace("_", " ");
            Level = level;
            Examples = new List<Example>();
            Contexts = new ContextCollection();
            this.isPending = isPending;
        }

        protected MethodInfo Method { get; set; }

        public Type Type { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public List<Example> Examples { get; set; }
        public ContextCollection Contexts { get; set; }
        public Action Before { get; set; }
        public Action Act { get; set; }
        public Action After { get; set; }
        public Context Parent { get; set; }
        public nspec NSpecInstance { get; set; }
        public Action<nspec> BeforeInstance { get; set; }
        public Action<nspec> ActInstance { get; set; }

        private bool isPending;
    }
}