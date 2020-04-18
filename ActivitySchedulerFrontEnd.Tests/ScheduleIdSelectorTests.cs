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
	public class ScheduleIdSelectorTests
	{
		private TestHost _host = new TestHost();
		private readonly List<string> _scheduleIdNames = new List<string> { "one", "two", "three" };
		private object _callBackValue;
		private RenderedComponent<ScheduleIdSelector> _component;

		[SetUp]
		public void Setup()
		{
			ISchedulerService service = Substitute.For<ISchedulerService>();
			service.GetScheduleIds().Returns(_scheduleIdNames);
			_host.AddService(service);

			EventCallbackFactory eventCallbackFactory = new EventCallbackFactory();
			EventCallback<string> eventCallback = eventCallbackFactory.Create<string>(this, x =>
			{
				_callBackValue = x;
			});
			IDictionary<string, object> parameters = new Dictionary<string, object>
			{
				{"CurrentScheduleIdChanged", eventCallback }
			};
			_component = _host.AddComponent<ScheduleIdSelector>(parameters);
		}

		[Test]
		public void SetSelector_ChangeValue_FiresCallback()
		{
			// Arrange
			_callBackValue = string.Empty;

			// Act
			HtmlAgilityPack.HtmlNode selector = _component.Find("select");
			string selectorValue = _scheduleIdNames[1];
			selector.Change(selectorValue);

			// Assert
			Assert.That(_callBackValue, Is.EqualTo(selectorValue), "Value on callback");
			Assert.That(_component.Instance.CurrentScheduleId, Is.EqualTo(selectorValue),
				"CurrentScheduleId");
		}
	}
}
