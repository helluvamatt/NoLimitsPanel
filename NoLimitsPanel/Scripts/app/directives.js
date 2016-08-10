/// <reference path="../jquery.signalR-2.2.1.js" />
/// <reference path="../angular.js" />
/// <reference path="../angular-touch.js" />

// directives.js
var app = angular.module('appDirectives', []);

// Directive to handle button triggers
app.directive('touchButton', ['$timeout', '$parse', function ($timeout, $parse)
{
	var compile = function (element, attr)
	{
		var touchFn = $parse(attr['touch'], null, true);
		var touchStartFn = $parse(attr['touchStart'], null, true);
		var touchEndFn = $parse(attr['touchEnd'], null, true);
		
		return function (scope, element)
		{
			var isWithinTimeout = false;

			var onTouchStart = function (e)
			{
				e.preventDefault();
				scope.$apply(function ()
				{
					isWithinTimeout = true;
					scope.touchTimeout = $timeout(function () { isWithinTimeout = false; }, 300);
					if (touchStartFn) touchStartFn(scope);
				});
			};

			var onTouchEnd = function (e)
			{
				e.preventDefault();
				scope.$apply(function ()
				{
					$timeout.cancel(scope.touchTimeout);
					if (touchEndFn) touchEndFn(scope);
					if (touchFn && isWithinTimeout) touchFn(scope);
				});
			};

			element.on('touchstart', onTouchStart);
			element.on('touchend', onTouchEnd);

			// Unbind events when element is destroyed
			scope.$on('$destroy', function ()
			{
				element.off('touchstart', onTouchStart);
				element.off('touchend', onTouchEnd);
				$timeout.cancel(scope.touchTimeout);
			});
		};
	};

	return {
		compile: compile,
		restrict: 'A',
	}

}]);