/// <reference path="../jquery-2.2.4.intellisense.js" />
/// <reference path="../jquery.signalR-2.2.1.js" />
/// <reference path="../angular.js" />
/// <reference path="../angular-touch.js" />
/// <reference path="enums.js" />
/// <reference path="directives.js" />

// controllers.js
var app = angular.module('app', ['appDirectives']);

app.controller('appController', ['$scope', '$attrs', '$timeout', function ($scope, $attrs, $timeout)
{
	//#region Scope variables

	// Import enums
	$scope.StationState = StationState;
	$scope.CoasterStyles = CoasterStyles;

	$scope.logEntries = [];
	$scope.isSignalRConnected = false;
	$scope.isConnected = false;
	$scope.isConnectDialogNeeded = false;
	$scope.isLoading = true;

	$scope.currentCoaster = -1;
	$scope.currentStation = -1;
	$scope.currentCoasterStyleId = "unknown";
	$scope.currentCoasterStyle = CoasterStyles[$scope.currentCoasterStyleId];
	$scope.currentStationState = 0;
	$scope.currentCoasterName = "Unknown";

	$scope.isEmergencyStop = false;
	$scope.isManualDispatch = false;
	$scope.isFlyingCarUnlocked = false;
	$scope.isFloorRaised = false;
	$scope.isHarnessOpen = false;
	$scope.isGatesOpen = false;

	$scope.canUnlockFlyingCar = false;
	$scope.canLockFlyingCar = false;
	$scope.canRaiseFloor = false;
	$scope.canLowerFloor = false;
	$scope.canOpenHarness = false;
	$scope.canCloseHarness = false;
	$scope.canOpenGates = false;
	$scope.canCloseGates = false;
	$scope.canDispatch = false;

	// Default connection details
	$scope.host = "localhost";
	$scope.port = 15151;

	//#endregion

	//#region Private variables

	var dispatchLeftPressed = false;
	var dispatchRightPressed = false;
	var dispatchLeftTimeout = null;
	var dispatchRightTimeout = null;

	//#endregion

	//#region SignalR setup

	var hubName = $attrs.hubName;
	$scope.hub = $.connection[hubName];

	//#region SignalR event handlers

	$.connection.hub.error(onSignalRFail);

	$.connection.hub.reconnecting(function ()
	{
		$scope.$apply(function ()
		{
			$scope.isSignalRConnected = false;
			$scope.isConnected = false;
			$scope.isLoading = true;
			debug("Reconnecting to SignalR...");
		});
	});

	$.connection.hub.reconnected(function ()
	{
		$scope.$apply(function ()
		{
			debug("SignalR reconnected.");
			$scope.isSignalRConnected = true;
			$scope.isLoading = false;
			$scope.hub.server.checkConnection().then(function (result)
			{
				$scope.$apply(function ()
				{
					$scope.isConnected = result;
					log("Connected...");
				});
			});
		});
	});

	$.connection.hub.disconnected(function ()
	{
		$scope.$apply(function ()
		{
			$scope.isSignalRConnected = false;
			$scope.isConnected = false;
			log("Disconnected. Press RESET.", true);
		});
	});

	$scope.hub.client.onError = function (error)
	{
		$scope.$apply(function ()
		{
			log(error.ErrorMessage, true);
			console.error(error);
		});
	};

	$scope.hub.client.onTelemetryReceived = function (telemetry)
	{
		$scope.$apply(function ()
		{
			var newStyle = telemetry.TelemetryData.CoasterStyle;
			if ($scope.currentCoasterStyleId != newStyle) {
				$scope.currentCoasterStyleId = newStyle;
				$scope.currentCoasterStyle = CoasterStyles[newStyle in CoasterStyles ? newStyle : "unknown"];
			}
		});
	};

	$scope.hub.client.onStationStateReceived = function (stationState)
	{
		$scope.$apply(function ()
		{
			var state = stationState.StationState;
			if ($scope.currentStationState != state) {
				$scope.currentStationState = state;

				$scope.isEmergencyStop = hasFlag(state, StationState.EmergencyStop);
				$scope.isManualDispatch = hasFlag(state, StationState.ManualDispatch);

				$scope.canUnlockFlyingCar = hasFlag(state, StationState.CanUnlockFlyerCar);
				$scope.canLockFlyingCar = hasFlag(state, StationState.CanLockFlyerCar);
				$scope.canRaiseFloor = hasFlag(state, StationState.CanRaisePlatform);
				$scope.canLowerFloor = hasFlag(state, StationState.CanLowerPlatform);
				$scope.canOpenHarness = hasFlag(state, StationState.CanOpenHarness);
				$scope.canCloseHarness = hasFlag(state, StationState.CanCloseHarness);
				$scope.canOpenGates = hasFlag(state, StationState.CanOpenGates);
				$scope.canCloseGates = hasFlag(state, StationState.CanCloseGates);
				$scope.canDispatch = hasFlag(state, StationState.CanDispatch);

				// These states must be calculated, as they are not available from the StationState
				$scope.isFlyingCarUnlocked = $scope.canLockFlyingCar;
				$scope.isFloorRaised = $scope.canLowerFloor;
				$scope.isHarnessOpen = $scope.canCloseHarness;
				$scope.isGatesOpen = $scope.canCloseGates;

			}
		});
	};

	$scope.hub.client.onCurrentCoasterOrStationChanged = function (state)
	{
		$scope.$apply(function ()
		{
			if ($scope.currentCoaster != state.CurrentCoasterAndStation.CurrentCoaster || $scope.currentStation != state.CurrentCoasterAndStation.CurrentStation) {
				$scope.currentCoaster = state.CurrentCoasterAndStation.CurrentCoaster;
				$scope.currentStation = state.CurrentCoasterAndStation.CurrentStation;
			}
		});
	};

	$scope.hub.client.onOkMessage = function (args)
	{
		$scope.$apply(function ()
		{
			debug("OkMessage received: " + args);
		});
	};

	$scope.hub.client.onVersionReceieved = function (args)
	{
		$scope.$apply(function ()
		{
			debug("Version received: " + args.Version);
		});
	};

	$scope.hub.client.onCoasterNameReceived = function (args)
	{
		$scope.$apply(function ()
		{
			$scope.currentCoasterName = args.CoasterName;
		});
	};

	//$scope.hub.client.onCoasterCountReceived = function (args) { };
	//$scope.hub.client.onCurrentCoasterAndStationReceived = function (args) {};

	//#endregion

	var onSignalRConnected = function ()
	{
		$scope.$apply(function ()
		{
			debug("Connected to SignalR.");
			$scope.isLoading = false;
			$scope.isSignalRConnected = true;
			$scope.isConnectDialogNeeded = true;
		});
	};

	var onSignalRConnectFail = function ()
	{
		$scope.$apply(function ()
		{
			$scope.isLoading = false;
			console.error("Failed to connect to SignalR.");
		});
	};

	var onSignalRDone = function (result)
	{
		$scope.$apply(function ()
		{
			debug("SignalR Done: " + result);
		});
	}

	var onSignalRFail = function (error)
	{
		$scope.$apply(function ()
		{
			log("Failed to communicate with the server: " + error, true);
			console.error('SignalR error: ' + error);
		});
	};

	//#endregion

	//#region Private methods

	var hasFlag = function (value, flag)
	{
		return (value & flag) == flag;
	}

	var debug = function (message)
	{
		if (console.debug) console.debug(message);
		else console.log(message);
	};

	var log = function (message, error)
	{
		$scope.logEntries.push({ message: message, timestamp: new Date(), error: error });
	};

	var isValidStation = function ()
	{
		return $scope.currentCoaster > -1 && $scope.currentStation > -1;
	};

	var doDispatch = function ()
	{
		if ($scope.isConnected && isValidStation()) {
			$scope.hub.server.dispatch($scope.currentCoaster, $scope.currentStation).done(onSignalRDone).fail(onSignalRFail);
		}
	}

	//#endregion

	//#region Scope-public methods

	$scope.reset = function ()
	{
		if ($scope.isSignalRConnected) {
			$scope.isConnectDialogNeeded = true;
		}
		else {
			debug("Connecting to SignalR...");
			$scope.isLoading = true;
			$.connection.hub.start().done(onSignalRConnected).fail(onSignalRConnectFail);
		}
	};

	$scope.connect = function ()
	{
		$scope.isConnectDialogNeeded = false;
		log("Connecting...");
		$scope.hub.server.connectTelemetry($scope.host, $scope.port).done(function (connected)
		{
			$scope.$apply(function ()
			{
				$scope.isConnected = connected;
				if (connected) log("Connected to Telemetry server.");
				else log("Failed to connect to Telemetry server.", true);
			});
		});
	};

	$scope.toggleEStop = function ()
	{
		if ($scope.isConnected) {
			$scope.hub.server.setEmergencyStop(!$scope.isEmergencyStop).done(onSignalRDone).fail(onSignalRFail);
		}
	};

	$scope.setGates = function (open)
	{
		if ($scope.isConnected && isValidStation()) {
			$scope.hub.server.setGates($scope.currentCoaster, $scope.currentStation, open).done(onSignalRDone).fail(onSignalRFail);
		}
	};

	$scope.setHarness = function (open)
	{
		if ($scope.isConnected && isValidStation()) {
			$scope.hub.server.setHarness($scope.currentCoaster, $scope.currentStation, open).done(onSignalRDone).fail(onSignalRFail);
		}
	};

	$scope.setPlatform = function (lowered)
	{
		if ($scope.isConnected && isValidStation()) {
			$scope.hub.server.setPlatform($scope.currentCoaster, $scope.currentStation, lowered).done(onSignalRDone).fail(onSignalRFail);
		}
	};

	$scope.setFlyerCar = function (locked)
	{
		if ($scope.isConnected && isValidStation()) {
			$scope.hub.server.setFlyerCar($scope.currentCoaster, $scope.currentStation, locked).done(onSignalRDone).fail(onSignalRFail);
		}
	};

	$scope.dispatchLeftTouchStart = function ()
	{
		dispatchLeftTimeout = $timeout(function ()
		{
			dispatchLeftPressed = true;
			if (dispatchLeftPressed && dispatchRightPressed) doDispatch();
		}, 2000);
	};

	$scope.dispatchRightTouchStart = function ()
	{
		dispatchRightTimeout = $timeout(function ()
		{
			dispatchRightPressed = true;
			if (dispatchLeftPressed && dispatchRightPressed) doDispatch();
		}, 2000);
	};

	$scope.dispatchLeftTouchEnd = function ()
	{
		dispatchLeftPressed = false;
		$timeout.cancel(dispatchLeftTimeout);
	};

	$scope.dispatchRightTouchEnd = function ()
	{
		dispatchRightPressed = false;
		$timeout.cancel(dispatchRightTimeout);
	};

	//#endregion

	//#region App startup

	debug("Connecting...");
	$.connection.hub.logging = true;
	$.connection.hub.start().done(onSignalRConnected).fail(onSignalRConnectFail);

	//#endregion

}]);
