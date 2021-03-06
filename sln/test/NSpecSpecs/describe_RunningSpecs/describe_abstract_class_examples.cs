﻿using System.Linq;
using NSpec;
using NSpec.Domain;
using NSpecSpecs.WhenRunningSpecs;
using NUnit.Framework;
using FluentAssertions;

namespace NSpecSpecs.describe_RunningSpecs
{
    [TestFixture]
    public class describe_abstract_class_examples : when_running_specs
    {
        abstract class AbstractClass : nspec
        {
            void specify_an_example_in_abstract_class()
            {
                Assert.That(true, Is.True);
            }
        }

        abstract class AnotherAbstractClassInChain : AbstractClass
        {
            void specify_an_example_in_another_abstract_class()
            {
                Assert.That(true, Is.True);
            }
        }

        class ConcreteClass : AnotherAbstractClassInChain
        {
            void specify_an_example()
            {
                Assert.That(true, Is.True);
            }
        }

        class DerivedConcreteClass : ConcreteClass
        {
            void specify_an_example_in_derived_concrete_class()
            {
                Assert.That(true, Is.True);
            }
        }

        [SetUp]
        public void Setup()
        {
            Run(new[] { typeof(DerivedConcreteClass), typeof(ConcreteClass), typeof(AbstractClass), typeof(AnotherAbstractClassInChain) });
        }

        [Test]
        public void abstracts_should_not_be_added_as_class_contexts()
        {
            var allClassContexts =
                contextCollection[0].AllContexts().Where(c => c is ClassContext).ToList();

            allClassContexts.Should().Contain(c => c.Name == "ConcreteClass");

            allClassContexts.Should().NotContain(c => c.Name == "AbstractClass");

            allClassContexts.Should().NotContain(c => c.Name == "AnotherAbstractClassInChain");
        }

        //TODO: specify that concrete classes must have an example of their own or they won't host
        //abstract superclass's examples or do away with abstract classes altogether .
        //I'm not sure this complexity is warranted.

        [Test]
        public void examples_of_abtract_classes_are_included_in_the_first_derived_concrete_class()
        {
            TheContext("ConcreteClass").Examples.Count().Should().Be(3);

            TheExample("specify an example in abstract class").ShouldHavePassed();

            TheExample("specify an example in another abstract class").ShouldHavePassed();
        }

        [Test]
        public void subsequent_derived_concrete_class_do_not_contain_the_examples_from_the_abtract_class()
        {
            TheContext("DerivedConcreteClass").Examples.Count().Should().Be(1);
        }
    }
}
