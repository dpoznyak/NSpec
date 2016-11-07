﻿using System;
using NSpec;
using NSpec.Domain;
using NSpecSpecs.WhenRunningSpecs;
using NUnit.Framework;
using FluentAssertions;

namespace NSpecSpecs.describe_RunningSpecs.Exceptions
{
    [TestFixture]
    [Category("RunningSpecs")]
    public class when_before_all_contains_exception : when_running_specs
    {
        class BeforeAllThrowsSpecClass : nspec
        {
            void method_level_context()
            {
                beforeAll = () => { throw new BeforeAllException(); };

                // just by its presence, this will enforce tests as it should never be reported
                afterAll = () => { throw new AfterAllException(); };

                it["should fail this example because of beforeAll"] = () => "1".Should().Be("1");

                it["should also fail this example because of beforeAll"] = () => "1".Should().Be("1");

                it["overrides exception from same level it"] = () => { throw new ItException(); };

                context["exception thrown by both beforeAll and nested before"] = () =>
                {
                    before = () => { throw new BeforeException(); };

                    it["overrides exception from nested before"] = () => "1".Should().Be("1");
                };

                context["exception thrown by both beforeAll and nested act"] = () =>
                {
                    act = () => { throw new ActException(); };

                    it["overrides exception from nested act"] = () => "1".Should().Be("1");
                };

                context["exception thrown by both beforeAll and nested it"] = () =>
                {
                    it["overrides exception from nested it"] = () => { throw new ItException(); };
                };

                context["exception thrown by both beforeAll and nested after"] = () =>
                {
                    it["overrides exception from nested after"] = () => "1".Should().Be("1");

                    after = () => { throw new AfterException(); };
                };
            }
        }

        [SetUp]
        public void setup()
        {
            Run(typeof(BeforeAllThrowsSpecClass));
        }

        [Test]
        public void the_example_level_failure_should_indicate_a_context_failure()
        {
            TheExample("should fail this example because of beforeAll")
                .Exception.GetType().Should().Be(typeof(ExampleFailureException));
            TheExample("should also fail this example because of beforeAll")
                .Exception.GetType().Should().Be(typeof(ExampleFailureException));
            TheExample("overrides exception from same level it")
                .Exception.GetType().Should().Be(typeof(ExampleFailureException));
            TheExample("overrides exception from nested before")
                .Exception.GetType().Should().Be(typeof(ExampleFailureException));
            TheExample("overrides exception from nested act")
                .Exception.GetType().Should().Be(typeof(ExampleFailureException));
            TheExample("overrides exception from nested it")
                .Exception.GetType().Should().Be(typeof(ExampleFailureException));
            TheExample("overrides exception from nested after")
                .Exception.GetType().Should().Be(typeof(ExampleFailureException));
        }

        [Test]
        public void examples_with_only_before_all_failure_should_fail_because_of_before_all()
        {
            TheExample("should fail this example because of beforeAll")
                .Exception.InnerException.GetType().Should().Be(typeof(BeforeAllException));
            TheExample("should also fail this example because of beforeAll")
                .Exception.InnerException.GetType().Should().Be(typeof(BeforeAllException));
        }

        [Test]
        public void it_should_throw_exception_from_before_all_not_from_same_level_it()
        {
            TheExample("overrides exception from same level it")
                .Exception.InnerException.GetType().Should().Be(typeof(BeforeAllException));
        }

        [Test]
        public void it_should_throw_exception_from_before_all_not_from_nested_before()
        {
            TheExample("overrides exception from nested before")
                .Exception.InnerException.GetType().Should().Be(typeof(BeforeAllException));
        }

        [Test]
        public void it_should_throw_exception_from_before_all_not_from_nested_act()
        {
            TheExample("overrides exception from nested act")
                .Exception.InnerException.GetType().Should().Be(typeof(BeforeAllException));
        }

        [Test]
        public void it_should_throw_exception_from_before_all_not_from_nested_it()
        {
            TheExample("overrides exception from nested it")
                .Exception.InnerException.GetType().Should().Be(typeof(BeforeAllException));
        }

        [Test]
        public void it_should_throw_exception_from_before_all_not_from_nested_after()
        {
            TheExample("overrides exception from nested after")
                .Exception.InnerException.GetType().Should().Be(typeof(BeforeAllException));
        }
    }
}
