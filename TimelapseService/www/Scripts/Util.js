///////////////////////////////////////////////////////////////
// Event Listeners ////////////////////////////////////////////
///////////////////////////////////////////////////////////////
/**
 * Adds a handler for multiple events to the element.
 * @param {any} ele The element.
 * @param {any} events Space-delimited list of event names.
 * @param {any} handler A handler function for all the events.
 */
export function AddEvents(ele, events, handler)
{
	events.split(" ").forEach(event =>
	{
		ele.addEventListener(event, handler);
	});
}
/**
 * Removes a handler for multiple events from the element.
 * @param {any} ele The element.
 * @param {any} events Space-delimited list of event names.
 * @param {any} handler A handler function that was previously registered.
 */
export function RemoveEvents(ele, events, handler)
{
	events.split(" ").forEach(event =>
	{
		ele.removeEventListener(event, handler);
	});
}
///////////////////////////////////////////////////////////////
// Misc ///////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////
String.prototype.padLeft = function (len, c)
{
	var pads = len - this.length;
	if (pads > 0)
	{
		var sb = [];
		var pad = c || "&nbsp;";
		for (var i = 0; i < pads; i++)
			sb.push(pad);
		sb.push(this);
		return sb.join("");
	}
	return this;
};

export function IE_GetDevicePixelRatio()
{
	return Math.sqrt(screen.deviceXDPI * screen.deviceYDPI) / 96;
}

export function GetDevicePixelRatio()
{
	var returnValue = window.devicePixelRatio || IE_GetDevicePixelRatio() || 1;
	if (returnValue <= 0)
		returnValue = 1;
	return returnValue;
}
export function Clamp(i, min, max)
{
	if (i < min)
		return min;
	if (i > max)
		return max;
	if (isNaN(i))
		return min;
	return i;
}
var escape = document.createElement('textarea');
export function EscapeHTML(html)
{
	escape.textContent = html;
	return escape.innerHTML;
}
export function UnescapeHTML(html)
{
	escape.innerHTML = html;
	return escape.textContent;
}
export function HtmlAttributeEncode(str)
{
	if (typeof str !== "string")
		return "";
	var sb = new Array("");
	for (var i = 0; i < str.length; i++)
	{
		var c = str.charAt(i);
		switch (c)
		{
			case '"':
				sb.push("&quot;");
				break;
			case '\'':
				sb.push("&#39;");
				break;
			case '&':
				sb.push("&amp;");
				break;
			case '<':
				sb.push("&lt;");
				break;
			case '>':
				sb.push("&gt;");
				break;
			default:
				sb.push(c);
				break;
		}
	}
	return sb.join("");
}
export function AppendArrays(a, b)
{
	var c = new Array(a.length + b.length);
	var i = 0;
	for (; i < a.length; i++)
		c[i] = a[i];
	for (var j = 0; j < b.length; i++ , j++)
		c[i] = b[j];
	return c;
}
export function getHiddenProp()
{
	var prefixes = ['webkit', 'moz', 'ms', 'o'];

	// if 'hidden' is natively supported just return it
	if ('hidden' in document) return 'hidden';

	// otherwise loop over all the known prefixes until we find one
	for (var i = 0; i < prefixes.length; i++)
	{
		if ((prefixes[i] + 'Hidden') in document)
			return prefixes[i] + 'Hidden';
	}

	// otherwise it's not supported
	return null;
}
export function documentIsHidden()
{
	var prop = getHiddenProp();
	if (!prop) return false;

	return document[prop];
}
export function GetFuzzyTime(ms)
{
	/// <summary>Gets a fuzzy time string accurate within 1 year.</summary>
	var years = Math.round(ms / 31536000000);
	if (years > 0)
		return years + " year" + (years === 1 ? "" : "s");
	var months = Math.round(ms / 2628002880);
	if (months > 0)
		return months + " month" + (months === 1 ? "" : "s");
	var weeks = Math.round(ms / 604800000);
	if (weeks > 0)
		return weeks + " week" + (weeks === 1 ? "" : "s");
	return GetFuzzyTime_Days(ms);
}
export function GetFuzzyTime_Days(ms)
{
	/// <summary>Gets a fuzzy time string accurate within 1 day.</summary>
	var days = Math.round(ms / 86400000);
	if (days > 0)
		return days + " day" + (days === 1 ? "" : "s");
	var hours = Math.round(ms / 3600000);
	if (hours > 0)
		return hours + " hour" + (hours === 1 ? "" : "s");
	var minutes = Math.round(ms / 60000);
	if (minutes > 0)
		return minutes + " minute" + (minutes === 1 ? "" : "s");
	return "less than 1 minute";
}
export function GetTimeStr(date, includeMilliseconds, use24HourTime)
{
	var ampm = "";
	var hour = date.getHours();
	if (!use24HourTime)
	{
		if (hour === 0)
		{
			hour = 12;
			ampm = " AM";
		}
		else if (hour === 12)
		{
			ampm = " PM";
		}
		else if (hour > 12)
		{
			hour -= 12;
			ampm = " PM";
		}
		else
		{
			ampm = " AM";
		}
	}
	var ms = includeMilliseconds ? ("." + date.getMilliseconds()) : "";

	var str = hour.toString().padLeft(2, '0') + ":" + date.getMinutes().toString().padLeft(2, '0') + ":" + date.getSeconds().toString().padLeft(2, '0') + ms + ampm;
	return str;
}
export function GetDateStr(date, includeMilliseconds)
{
	var str = date.getFullYear() + "/" + (date.getMonth() + 1) + "/" + date.getDate() + " " + GetTimeStr(date, includeMilliseconds);
	return str;
}
export function GetScrollParent(element, includeHidden)
{
	let style = getComputedStyle(element);
	let excludeStaticParent = style.position === "absolute";
	let overflowRegex = includeHidden ? /(auto|scroll|hidden)/ : /(auto|scroll)/;

	if (style.position === "fixed")
		return document.documentElement;
	let parent = element;
	while ((parent = parent.parentElement))
	{
		style = getComputedStyle(parent);
		if (excludeStaticParent && style.position === "static")
			continue;
		if (parent.tagName === "BODY")
			continue; // Body isn't scrollable (at least not in Chrome 70)
		if (overflowRegex.test(style.overflow + style.overflowY + style.overflowX))
			return parent;
	}

	return document.documentElement;
}