import * as Util from 'appRoot/scripts/Util.js';

export default function ToasterHelper(toastMethod)
{
	this.error = this.Error = function (title, message, duration, format)
	{
		if (!duration)
			duration = 15000;
		makeToast('error', title, message, duration, format);
	};
	this.warning = this.Warning = function (title, message, duration, format)
	{
		makeToast('warning', title, message, duration, format);
	};
	this.info = this.Info = function (title, message, duration, format)
	{
		makeToast('info', title, message, duration, format);
	};
	this.success = this.Success = function (title, message, duration, format)
	{
		makeToast('success', title, message, duration, format);
	};
	function makeToast(type, title, message, duration, format)
	{
		if (!duration)
			duration = 3000;
		if (!message)
		{
			message = title;
			title = null;
		}
		if (typeof message === "object" && typeof message.message === "string" && typeof message.stack === "string")
		{
			console.error(type + " toast", message);
			message = message.message + ": " + message.stack;
		}
		else if (typeof message === "object" && typeof message.name === "string" && typeof message.message === "string" && typeof message.code === "number")
		{
			message = message.name + " (code " + message.code + "): " + message.message, message;
			console.error(type + " toast", message);
		}
		else
		{
			if (type === "error")
				console.error(type + " toast: ", message);
			else
				console.log(type + " toast: ", message);
		}
		if (format !== "html")
			message = Util.EscapeHTML(message);
		toastMethod('add', {
			title: title,
			msg: message,
			timeout: duration,
			type: type
		});
	}
}