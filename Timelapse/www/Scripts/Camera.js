function Navigate(path)
{
	$("#navmenuwrapper").load("Navigation?path=" + encodeURIComponent(path) + "&cam=" + camId, function ()
	{
		doResize();
	});
	$("#ajax-loader-t").hide();
}
function Img(linkIdx)
{
	var jqLink = $("#imglnk" + linkIdx);
	path = jqLink.attr("href");
	currentImgLinkIdx = parseInt(linkIdx);
	if (currentImgSrc != path)
	{
		$("#imgFrame").attr("src", path);
		currentImgSrc = path;
		var jqLink = $("#imglnk" + linkIdx);
		currentImgTime = jqLink.html();
		var ofst = jqLink.offset();
		$("#ajax-loader-t").insertAfter(jqLink);
		$("#ajax-loader-t").show();
		if (!slideshowActive)
			$("#imgtime").html("...");
	}
}
function startSlideshow()
{
	$("#SlideshowStartButton").hide();
	$("#SlideshowStopButton").show();
	slideshowActive = true;
	SlideshowNextImg();
}
function stopSlideshow()
{
	$("#SlideshowStartButton").show();
	$("#SlideshowStopButton").hide();
	slideshowActive = false;
}
function SlideshowNextImg()
{
	if (!slideshowActive)
		return;
	if (currentImgLinkIdx <= 0)
	{
		// Get next day of images
		$("#ajax-loader-t").hide();
		$("#navmenuwrapper").load("NavigationNextDay?path=" + encodeURIComponent($("#navheader .directorypath").text()) + "&cam=" + camId, function ()
		{
			var links = $("#navlinks a");
			if (links.length > 0)
			{
				currentImgLinkIdx = links.length;
				SlideshowNextImg();
			}
			else
			{
				stopSlideshow();
				$("#SlideshowStartButton").hide();
			}
			doResize();
		});
	}
	else
	{
		Img(currentImgLinkIdx - 1);
	}
}
var slideshowActive = false;
var currentImgLinkIdx = 0;
var currentImgSrc = "";
var resizeTimeout = null;
var zoomHintTimeout = null;
var digitalZoom = 0;
var zoomTable = [0, 1, 1.2, 1.4, 1.6, 1.8, 2, 2.5, 3, 3.5, 4, 4.5, 5, 6, 7, 8, 9, 10, 12, 14, 16, 18, 20, 23, 26, 30, 35, 40, 45, 50];
var imageIsDragging = false;
var imageIsLargerThanAvailableSpace = false;
var mouseX = 0;
var mouseY = 0;
var imgDigitalZoomOffsetX = 0;
var imgDigitalZoomOffsetY = 0;
var previousImageDraw = new Object();
var originwidth = 1280;
var originheight = 720;
previousImageDraw.x = -1;
previousImageDraw.y = -1;
previousImageDraw.w = -1;
previousImageDraw.h = -1;
previousImageDraw.z = 10;

$(function ()
{
	$("#imgFrame").load(function ()
	{
		if (typeof this.naturalWidth == "undefined" || this.naturalWidth == 0)
		{
			alert('Bad image data was received.');
		}
		else
		{
			if (this.naturalWidth != originwidth || this.naturalHeight != originheight)
			{
				originwidth = this.naturalWidth;
				originheight = this.naturalHeight;
				doResize();
			}
			$("#imgtime").html(currentImgTime);
			$("#imgtime").css("min-width", $("#imgtime").width() + "px");
			if ($("#imgFrame").attr("src") == currentImgSrc)
				$("#ajax-loader-t").hide();
			if (slideshowActive)
				SlideshowNextImg();
		}
	});
	$("#imgFrame").error(function ()
	{
		alert('Failed to load image.');
	});
	if (imgFrameGradientBg)
	{
		$("#camCell").css("background", "linear-gradient(135deg, #131313 0%,#1c1c1c 9%,#2b2b2b 18%,#111111 28%,#000000 38%,#2c2c2c 56%,#474747 68%,#666666 80%,#595959 90%,#4c4c4c 100%)");
	}
});
function PopupMessage(msg)
{
	var pm = $("#popupMessage");
	if (pm.length < 1)
		$("#outerFrame").after('<div id="popupFrame"><div id="popupMessage">' + msg + '</div><center><input type="button" value="Close Message" onclick="CloseMessage()"/></center></div>');
	else
		pm.append("<br/>" + msg);
}
function CloseMessage()
{
	$("#popupFrame").remove();
}
$(window).load(doResize);
$(window).resize(doResize);
function doResize()
{
	resize(false);
}
function resize(wasCausedByTrigger)
{
	var windowW = $(window).width(), windowH = $(window).height();

	var topMenuHeight = $("#topMenu").outerHeight(true);
	var leftMenuWidth = $("#leftMenu").outerWidth(true);
	var camCellWidth = (windowW - leftMenuWidth);

	$("#topMenu").css("width", camCellWidth + "px");
	$("#topMenu").css("left", leftMenuWidth + "px");
	$("#camCell").css("width", camCellWidth + "px");
	$("#camCell").css("height", (windowH - topMenuHeight) + "px");
	$("#camCell").css("top", topMenuHeight + "px");
	$("#camCell").css("left", leftMenuWidth + "px");
	$("#leftMenu").css("height", windowH + "px");

	var menuPadding = $("#leftMenuInner").outerHeight(true) - $("#leftMenuInner").height();
	$("#navlinks").css("height", (windowH - (menuPadding + $("#navheader").outerHeight(true) + $("#cameraNameDiv").outerHeight(true))) + "px");

	ImgResized();
}
function ImgResized()
{
	var imgAvailableWidth = $("#camCell").width();
	var imgAvailableHeight = $("#camCell").height();

	// Calculate new size based on zoom levels
	var imgDrawWidth = originwidth * (zoomTable[digitalZoom]);
	var imgDrawHeight = originheight * (zoomTable[digitalZoom]);
	if (imgDrawWidth == 0)
	{
		imgDrawWidth = imgAvailableWidth;
		imgDrawHeight = imgAvailableHeight;

		var originRatio = originwidth / originheight;
		var newRatio = imgDrawWidth / imgDrawHeight;
		if (newRatio < originRatio)
			imgDrawHeight = imgDrawWidth / originRatio;
		else
			imgDrawWidth = imgDrawHeight * originRatio;
	}
	$("#imgFrame").css("width", imgDrawWidth + "px");
	$("#imgFrame").css("height", imgDrawHeight + "px");

	imageIsLargerThanAvailableSpace = imgDrawWidth > imgAvailableWidth || imgDrawHeight > imgAvailableHeight;

	if (previousImageDraw.z > -1 && previousImageDraw.z != digitalZoom)
	{
		// We just experienced a zoom change
		// Find the mouse position percentage relative to the center of the image at its old size
		var imgPos = $("#imgFrame").position();
		var leftMenuWidth = $("#leftMenu").outerWidth(true);
		var topMenuHeight = $("#topMenu").outerHeight(true);
		var mouseRelX = -0.5 + (parseFloat((mouseX - leftMenuWidth) - imgPos.left) / previousImageDraw.w);
		var mouseRelY = -0.5 + (parseFloat((mouseY - topMenuHeight) - imgPos.top) / previousImageDraw.h);
		// Get the difference in image size
		var imgSizeDiffX = imgDrawWidth - previousImageDraw.w;
		var imgSizeDiffY = imgDrawHeight - previousImageDraw.h;
		// Modify the zoom offsets by % of difference
		imgDigitalZoomOffsetX -= mouseRelX * imgSizeDiffX;
		imgDigitalZoomOffsetY -= mouseRelY * imgSizeDiffY;
	}

	// Enforce digital panning limits
	var maxOffsetX = (imgDrawWidth - imgAvailableWidth) / 2;
	if (maxOffsetX < 0)
		imgDigitalZoomOffsetX = 0;
	else if (imgDigitalZoomOffsetX > maxOffsetX)
		imgDigitalZoomOffsetX = maxOffsetX;
	else if (imgDigitalZoomOffsetX < -maxOffsetX)
		imgDigitalZoomOffsetX = -maxOffsetX;

	var maxOffsetY = (imgDrawHeight - imgAvailableHeight) / 2;
	if (maxOffsetY < 0)
		imgDigitalZoomOffsetY = 0;
	else if (imgDigitalZoomOffsetY > maxOffsetY)
		imgDigitalZoomOffsetY = maxOffsetY;
	else if (imgDigitalZoomOffsetY < -maxOffsetY)
		imgDigitalZoomOffsetY = -maxOffsetY;

	// Calculate new image position
	var proposedX = (((imgAvailableWidth - imgDrawWidth) / 2) + imgDigitalZoomOffsetX);
	var proposedY = (((imgAvailableHeight - imgDrawHeight) / 2) + imgDigitalZoomOffsetY);

	$("#imgFrame").css("left", proposedX + "px");
	$("#imgFrame").css("top", proposedY + "px");

	// Store new image position for future calculations
	previousImageDraw.x = proposedX;
	previousImageDraw.x = proposedY;
	previousImageDraw.w = imgDrawWidth;
	previousImageDraw.h = imgDrawHeight;
	previousImageDraw.z = digitalZoom;
}
$(function ()
{
	$('#camCell').mousewheel(function (e, delta, deltaX, deltaY)
	{
		e.preventDefault();
		if (deltaY < 0)
			digitalZoom -= 1;
		else if (deltaY > 0)
			digitalZoom += 1;
		if (digitalZoom < 0)
			digitalZoom = 0;
		else if (digitalZoom >= zoomTable.length)
			digitalZoom = zoomTable.length - 1;

		$("#zoomhint").stop(true, true);
		$("#zoomhint").show();
		$("#zoomhint").html(digitalZoom == 0 ? "Fit" : (zoomTable[digitalZoom] + "x"))
		RepositionZoomHint();
		if (zoomHintTimeout != null)
			clearTimeout(zoomHintTimeout);
		zoomHintTimeout = setTimeout(function () { $("#zoomhint").fadeOut() }, 200);

		ImgResized();
	});
	$('#camCell,#zoomhint').mousedown(function (e)
	{
		mouseX = e.pageX;
		mouseY = e.pageY;
		imageIsDragging = true;
		e.preventDefault();
	});
	$(document).mouseup(function (e)
	{
		imageIsDragging = false;

		mouseX = e.pageX;
		mouseY = e.pageY;
	});
	$('#camCell').mouseleave(function (e)
	{
		var ofst = $("#camCell").offset();
		if (e.pageX < ofst.left || e.pageY < ofst.top || e.pageX >= ofst.left + $("#camCell").width() || e.pageY >= ofst.top + $("#camCell").height())
		{
			imageIsDragging = false;
		}
		mouseX = e.pageX;
		mouseY = e.pageY;
	});
	$(document).mouseleave(function (e)
	{
		imageIsDragging = false;
	});
	$(document).mousemove(function (e)
	{
		var requiresImgResize = false;
		if (imageIsDragging && imageIsLargerThanAvailableSpace)
		{
			imgDigitalZoomOffsetX += (e.pageX - mouseX);
			imgDigitalZoomOffsetY += (e.pageY - mouseY);
			requiresImgResize = true;
		}

		mouseX = e.pageX;
		mouseY = e.pageY;

		if (requiresImgResize)
			ImgResized();

		if ($("#zoomhint").is(":visible"))
			RepositionZoomHint();
	});
});
function RepositionZoomHint()
{
	$("#zoomhint").css("left", (mouseX - $("#zoomhint").outerWidth(true)) + "px").css("top", (mouseY - $("#zoomhint").outerHeight(true)) + "px");
}