﻿using System;
using System.Linq;
using NUnit.Framework;
using NSpec;
using NSpec.Domain;

namespace NSpecSpecs.WhenRunningSpecs
{
    [TestFixture]
    public class when_after_contains_exception : when_running_specs
    {
        class SpecClass : nspec
        {
            void method_level_context()
            {
                after = () => { throw new InvalidOperationException(); };

                it["should fail this example because of after"] = () => "1".should_be("1");

                it["should also fail this example because of after"] = () => "1".should_be("1");
            }
        }

        [SetUp]
        public void setup()
        {
            Run(typeof(SpecClass));
        }

        [Test]
        public void it_should_fail_all_examples_in_after()
        {
            TheExample("should fail this example because of after").Exception.GetType().should_be(typeof(InvalidOperationException));
            TheExample("should also fail this example because of after").Exception.GetType().should_be(typeof(InvalidOperationException));
        }

        private Example TheExample(string name)
        {
            return classContext.Contexts.First().AllExamples().Single(s => s.Spec == name);
        }
    }
}