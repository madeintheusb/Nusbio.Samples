/*
    LambdaFunctionsFredOffice
    ARN - arn:aws:lambda:us-east-1:829312865452:function:FredOffice

    Alexa start office lamp 
    Turn office lamp On
    Turn office lamp Off
    what are the lamps state
*/
var mqtt = require('mqtt');
require("Sys");
require("String");

var __context = null;
var __newState = "unknown";

var MQTT_SERVER = "mqtt://test.mosquitto.org";
var MQTT_FredOfficeChannelID = "BD06";
var _mqttClient = null;

function getMqttClient(allocate) {

    //if((allocate===true)||(_mqttClient === null)
    _mqttClient = mqtt.connect(MQTT_SERVER);
    return _mqttClient;
}

function getMqttChannel() {

    return MQTT_FredOfficeChannelID + "/office/lamp";
}

function mqttPublish(channel, data) {

    console.log("MQTT Publishing {0} {1}".format(channel, data));
    _mqttClient.publish(channel, data);
}

function callback_FinalizeRequest(sessionAttributes, speechletResponse) {

    __context.succeed(buildResponse(sessionAttributes, speechletResponse));
}

function callback_OnIntentRequest(sessionAttributes, speechletResponse) {

    getMqttClient(true);
    _mqttClient.on('connect', function () {

        mqttPublish(getMqttChannel(), __newState);
        callback_FinalizeRequest(sessionAttributes, speechletResponse);
        _mqttClient.end();
    });
}

var ALEXA_SKILL_APP_ID = "amzn1.echo-sdk-ams.app.03a7";
var INTENT = "TurnLampOfficeIntent";
var WHAT_INTENT = "WhatsLampOfficeStateIntent";

// Route the incoming request based on type (LaunchRequest, IntentRequest,
// etc.) The JSON body of the request is provided in the event parameter.
exports.handler = function (event, context) {

    __context = context;

    try {

        if (event.session.new) {

            onSessionStarted({ requestId: event.request.requestId }, event.session);
        }

        if (event.request.type === "LaunchRequest") {

            onLaunch(event.request, event.session, callback_FinalizeRequest);
        }
        else if (event.request.type === "IntentRequest") {

            onIntent(event.request, event.session, callback_OnIntentRequest);
        }
        else if (event.request.type === "SessionEndedRequest") {

            onSessionEnded(event.request, event.session);
            context.succeed();
        }
    } catch (e) {
        context.fail("Exception: " + e);
    }
};

function onSessionStarted(sessionStartedRequest, session) {

    console.log("onSessionStarted requestId=" + sessionStartedRequest.requestId + ", sessionId=" + session.sessionId);
}

function onLaunch(launchRequest, session, callback) {

    getWelcomeResponse(callback);
}

function onIntent(intentRequest, session, callback) {

    //console.log("onIntent requestId=" + intentRequest.requestId + ", sessionId=" + session.sessionId);

    var intent = intentRequest.intent;
    var intentName = intentRequest.intent.name;

    console.log("onIntent intentName=" + intentName);

    // Dispatch to your skill's intent handlers
    if (INTENT === intentName) {
        setLampOnOffInSession(intent, session, callback);
    }
    else if (WHAT_INTENT === intentName) {
        getStateFromSession(intent, session, callback);
    }
    else if ("AMAZON.HelpIntent" === intentName) {
        getWelcomeResponse(callback);
    }
    else if ("AMAZON.StopIntent" === intentName || "AMAZON.CancelIntent" === intentName) {
        handleSessionEndRequest(callback);
    }
    else {
        throw "Invalid intent";
    }
}

function onSessionEnded(sessionEndedRequest, session) {

    // Clean up
    console.log("onSessionEnded requestId=" + sessionEndedRequest.requestId + ", sessionId=" + session.sessionId);
}

// --------------- Functions that control the skill's behavior -----------------------

function getWelcomeResponse(callback) {

    // If we wanted to initialize the session to have some attributes we could add those here.
    var sessionAttributes = {};
    var cardTitle = "Welcome";
    var speechOutput = "Office lamp automation";
    var repromptText = "Should the lamps be turned on or off"; // If the user either does not reply to the welcome message or says something that is not understood, they will be prompted again with this text.
    var shouldEndSession = false;

    callback(sessionAttributes, buildSpeechletResponse(cardTitle, speechOutput, repromptText, shouldEndSession));
}

function handleSessionEndRequest(callback) {

    var cardTitle = "Session Ended";
    var speechOutput = "Thank you for trying the Alexa Skills Kit sample. Have a nice day!";
    var shouldEndSession = true; // Setting this to true ends the session and exits the skill.

    callback({}, buildSpeechletResponse(cardTitle, speechOutput, null, shouldEndSession));
}

function setLampOnOffInSession(intent, session, callback) {

    var cardTitle = intent.name;
    var stateSlot = intent.slots.State;
    var repromptText = "";
    var sessionAttributes = {};
    var shouldEndSession = true; // If you want to continue say false
    var speechOutput = "";

    console.log("setLampOnOffInSession stateSlot:" + stateSlot);

    if (stateSlot) {

        var state = stateSlot.value;
        __newState = state;
        sessionAttributes = createStateAttributes(state);
        speechOutput = "The state of the lamp is now {0}.".format(state);
        repromptText = "bla bla one";

        if (!shouldEndSession) {
            speechOutput += " You could ask me or not what is the state";
        }
    }
    else {
        speechOutput = "I'm not sure how you would like the lamps to be set. Please try again";
        repromptText = "I'm not sure how do you want the lamps to be set. Please try again";
        shouldEndSession = false;
    }
    callback(sessionAttributes, buildSpeechletResponse(cardTitle, speechOutput, repromptText, shouldEndSession));
}

function createStateAttributes(state) {
    return {
        state: state
    };
}

function getStateFromSession(intent, session, callback) {

    var state;
    var repromptText = null;
    var sessionAttributes = {};
    var shouldEndSession = false;
    var speechOutput = "";

    if (session.attributes) {

        state = session.attributes.state;
        __newState = state;
    }

    if (state) {

        speechOutput = "The lamps in your office are " + state + ". I'am done now.";
        shouldEndSession = true;
    }
    else {
        speechOutput = "I'm not sure what are the state of the lamps in your office";
    }

    // Setting repromptText to null signifies that we do not want to reprompt the user.
    // If the user does not respond or says something that is not understood, the session
    // will end.
    callback(sessionAttributes, buildSpeechletResponse(intent.name, speechOutput, repromptText, shouldEndSession));
}

// --------------- Helpers that build all of the responses -----------------------

function buildSpeechletResponse(title, output, repromptText, shouldEndSession) {
    return {
        outputSpeech: {
            type: "PlainText",
            text: output
        },
        card: {
            type: "Simple",
            title: "SessionSpeechlet - " + title,
            content: "SessionSpeechlet - " + output
        },
        reprompt: {
            outputSpeech: {
                type: "PlainText",
                text: repromptText
            }
        },
        shouldEndSession: shouldEndSession
    };
}

function buildResponse(sessionAttributes, speechletResponse) {
    return {
        version: "1.0",
        sessionAttributes: sessionAttributes,
        response: speechletResponse
    };
}



/*
        //console.log("event.session.application.applicationId=" + event.session.application.applicationId);
        if (event.session.application.applicationId !== "amzn1.echo-sdk-ams.app.[unique-value-here]") {
             context.fail("Invalid Application ID");
        }

*/