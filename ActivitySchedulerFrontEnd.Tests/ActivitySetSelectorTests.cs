using ActivitySchedulerFrontEnd.Pages;
using ActivitySchedulerFrontEnd.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Testing;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;

namespace ActivitySchedulerFrontEnd.Tests
{
	[TestFixture]
	public class ActivitySetSelectorTests
	{
		private TestHost _host = new TestHost();
		private readonly List<string> _activitySetNames = new List<string> { "one", "two", "three" };
		private object _callBackValue;
		private RenderedComponent<ActivitySetSelector> _component;

		[SetUp]
		public void Setup()
		{
			IActivityDefinitionService service = Substitute.For<IActivityDefinitionService>();
			service.GetActivitySetNames().Returns(_activitySetNames);
			_host.AddService(service);

			EventCallbackFactory eventCallbackFactory = new EventCallbackFactory();
			EventCallback<string> eventCallback = eventCallbackFactory.Create<string>(this, x =>
			{
				_callBackValue = x;
			});
			IDictionary<string, object> parameters = new Dictionary<string, object>
			{
				{"CurrentActivitySetChanged", eventCallback }
			};
			_component = _host.AddComponent<ActivitySetSelector>(parameters);
		}

		[Test]
		public void SetSelector_ChangeValue_FiresCallback()
		{
			// Arrange
			_callBackValue = string.Empty;

			// Act
			var selector = _component.Find("select");
			var selectorValue = _activitySetNames[1];
			selector.Change(selectorValue);

			// Assert
			Assert.That(_callBackValue, Is.EqualTo(selectorValue), "Value on callback");
			Assert.That(_component.Instance.CurrentActivitySet, Is.EqualTo(selectorValue),
				"CurrentActivitySet");
		}
	}
}
