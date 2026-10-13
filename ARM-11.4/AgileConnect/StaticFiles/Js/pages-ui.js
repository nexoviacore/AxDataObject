var repeatArray = new Array();
var refreshFldsHTML = new Object();
var rownumList = new Object();
var apiCallsCount = 0;

$(document).ready(function () {
	doPageLoadActions();
	addPopupsHTML();
})

function doPageLoadActions() {
	getRefreshFldsHTML();
	loadPageData();
	loadDataFromAPIs();
	bindEvents();
	displayMainLoader("hide");
}

function bindEvents() {
	$(".openPopup").unbind('click').click(function () {
		var pageId = $(this).attr("data-target");
		var pageTitle = $(this).attr("data-title");

		loadHTML(pageId, pageTitle, "popup");
	});

	$(".openInline").unbind('click').click(function () {
		openInline(this);
	});

	$(".uploadFile").unbind('click').click(function () {
		var file = $(this).attr("data-file-target");
		var filePath = $(this).attr("data-file-path");
		uploadFile(file, filePath);
	});

	$(".axsave").unbind('click').click(function () {
		var pageId = $(this).attr("data-target");
		saveData(pageId, false);
	});

	$(".axsaveasync").unbind('click').click(function () {
		var pageId = $(this).attr("data-target");
		saveData(pageId, true);
	});

	$(".axrefresh:not(.ondemand)").unbind('click').click(function () {
		var elem = $(this);
		refreshHTML(elem);
	});

	$(".axcopyhtml").unbind('click').click(function () {
		var elem = $(this);
		var sourceId = elem.attr("data-sourceid");
		var sourceHtml = $(elem.attr("data-source")).outerHTML();
		var targetElem = elem.attr("data-target");
		copyHTML(sourceId, sourceHtml, targetElem );
	});

	$('.axremovehtml').unbind('click').click(function (e) {
		var elem = $(this);
		$(elem.attr("data-target")).remove();
	});

	$('.axlogout').unbind('click').click(function (e) {
		logout();
	});	
}



function openInline(currElem) {
	var pageId = $(currElem).attr("data-target");
	var targetElem = $(currElem).attr("data-elem");
	if (targetElem == "#divHome")
		displayMainLoader("show");

	setMobileViewFlag();
	if (typeof localStorage.getItem('RCP_MobileView') != "undefined" && localStorage.getItem('RCP_MobileView') == "true") {
		var pageIdMobile = $(currElem).attr("data-target-mobile");
		if (typeof pageIdMobile != "undefined")
			pageId = pageIdMobile;
	}
	loadHTML(pageId, "", targetElem, currElem);
}

function refreshHTML(elem) {
	var fldId = elem.attr("data-target");
	var refresh = elem.attr("data-refresh");
	if (typeof refresh != "undefined") {
		refreshHTMLElement(fldId, (refresh.toLowerCase() == "true" ? true : false));
	}
	else
		refreshHTMLElement(fldId, false);
}

function refreshHTMLElement(fldId, refresh) {
	var fld = $("#" + fldId);
	if (fld.length > 0) {
		fld.replaceWith(refreshFldsHTML[fldId]);
		$("#" + fldId + " .axdata," + "#" + fldId + " .axdata-ondemand").each(function () {
			var datasource = $(this).attr("data-source");
			if (typeof datasource != "undefined") {
				loadData({
					dataId: datasource,
					elem: $(this),
					refresh: refresh,
					showLoader: true
				});
			}
		});
	}	
}

function refreshHTMLElementOnDemand(fldId) {
	var fld = $("#" + fldId);
	if (fld.length > 0) {
		fld.replaceWith(refreshFldsHTML[fldId]);
		$("#" + fldId + " .axdata-ondemand").each(function () {
			var datasource = $(this).attr("data-source");
			if (typeof datasource != "undefined") {
				loadData({
					dataId: datasource,
					elem: $(this),
					refresh: false,
					showLoader: true
				});
			}
		});
	}
}

function loadPageData() {

	$(".axpagedata:not(.axrepeat,.axrepeat-innerhtml)").each(function () {
		doLoadValues($(this), $(this).attr("data-source"));
	});

	$(".axrepeat.axpagedata:not(.ondemand)").each(function () {
		repeatArray.push($(this).attr("data-source"));
	});

	for (var i = 0; i < repeatArray.length; i++) {
		$(".axrepeat.axpagedata[data-source='" + repeatArray[i] +"']:not(.ondemand)").each(function () {
			doRepeaterLoad($(this), $(this).attr("data-source"));
		});
	}

	$(".axrepeat-innerhtml.axpagedata").each(function () {
		doRepeaterLoad($(this), $(this).attr("data-source"));
	});
}

function getRefreshFldsHTML() {
	$(".axrefresh").each(function () {
		let fldId = $(this).attr("data-target");
		if (typeof refreshFldsHTML[fldId] == 'undefined') {
			refreshFldsHTML[fldId] = $("#" + fldId).outerHTML();
		}
	});
}

//Plugin to get complete HTML
jQuery.fn.outerHTML = function () {
	return (this[0]) ? this[0].outerHTML : '';
};


function doLoadValues(elem, dataObj) {
	dataObj = eval(dataObj);
	var elemHtml = elem.outerHTML();

	if (elemHtml.indexOf("{{") > -1) {
		for (var key in dataObj[0]) {
			if (dataObj[0].hasOwnProperty(key) && elemHtml.indexOf("{{" + key + "}}") > -1) {
				var val = dataObj[0][key];
				elemHtml = elemHtml.replaceAll("{{" + key + "}}", val);
			}
		}
	}

	elem.replaceWith(elemHtml);
}

function doRepeaterLoad(elem, dataObj) {
	dataObj = eval(dataObj);

	var elemHtml = "";
	if (elem.hasClass("axrepeat-innerhtml")) {
		elemHtml = elem.html();
	}
	else
		elemHtml = elem.outerHTML();
	
	var tempHtml = elemHtml;
	var finalHtml = "";

	for (var i = 0; i < dataObj.length; i++) {
		if (typeof elem.attr("data-filter") != "undefined") {
			var filter = elem.attr("data-filter");
			filter = filter.split('=');
			if (dataObj[i][filter[0]] == filter[1] && tempHtml.indexOf("{{") > -1) {
				for (var key in dataObj[i]) {
					if (dataObj[i].hasOwnProperty(key) && tempHtml.indexOf("{{" + key + "}}") > -1) {
						var val = dataObj[i][key];
						tempHtml = tempHtml.replaceAll("{{" + key + "}}", val);
					}
				}

				finalHtml += tempHtml;
				tempHtml = elemHtml;
				break;
			}
		}
		else {
			for (var key in dataObj[i]) {
				if (dataObj[i].hasOwnProperty(key) && tempHtml.indexOf("{{" + key + "}}") > -1) {
					var val = dataObj[i][key];
					tempHtml = tempHtml.replaceAll("{{" + key + "}}", val);
				}
			}

			finalHtml += tempHtml;
			tempHtml = elemHtml;
		}
		
	}

	if (elem.hasClass("axrepeat-innerhtml")) {
		elem.html(finalHtml);
	}
	else
		elem.replaceWith(finalHtml);
}

function openPopup(defId){
    $('#' + defId).modal();
}

function loadHTML(pageId, pageTitle, target, currElem) {	
	var pageObj = new Object();
	pageObj.pageId = pageId;
	pageObj.pageTitle = pageTitle;
	pageObj.pageTarget = target;

	displayLoader("show");

	try {
		pageObj = beforeLoadHTML(pageObj);
	}
	catch (ex) { }

	$.ajax({
		type: "GET",
		url: "../pages/" + pageObj.pageId,
		headers: { "Authorization": localStorage.getItem('RCP_JwtToken') },
		cache: false,
		async: true,
		dataType: "html",
		success: function (data) {
			loadHTML_Success(pageObj.pageId, pageObj.pageTitle, data, pageObj.pageTarget, currElem);
		},
		error: function (request, status, error) {
			displayLoader("hide");
			console.log(request);
			alert(request.responseText);
		}
	});
}

function replaceIframeContent(iframeElement, newHTML) {
	iframeElement.src = "about:blank";
	iframeElement.contentWindow.document.open();
	iframeElement.contentWindow.document.write(newHTML);
	iframeElement.contentWindow.document.close();
}

function loadHTML_Success(pageId, pageTitle, data, target, currElem) {
	if (target == "popup") {
		if ($("#modal_" + pageId).length > 0) {
			$("#modal_" + pageId).remove();
		}

		$('body').append(`
		<div class="modal fade" id="modal_`+ pageId + `" role="dialog" aria-labelledby="exampleModalLabel" aria-hidden="true" tabindex = "-1" >
			<div class="modal-dialog modal-lg" role="document">
				<div class="modal-content">
					<div class="modal-header bg-pink col-md-12">
						<h5 class="modal-title" id="exampleModalLongTitle">`+ pageTitle + `</h5> <button
							type="button" class="close" data-dismiss="modal" aria-label="Close"> <span
								aria-hidden="true">&times;</span> </button>
					</div>
					<div class="modal-body">
						<iframe id="iframe_`+ pageId + `" style="height:100%;width:100%"></iframe>
					</div>
				</div>
			</div>
		</div>
		`);
		//$("#iframe_" + pageId).contents().find('html').html(data);

		replaceIframeContent(document.getElementById("iframe_" + pageId), data)

		$("#modal_" + pageId).on('hidden.bs.modal', function (e) {
			var elem = $(this);
			elem.remove();
		})

		$("#modal_" + pageId).modal();
	}
	else {
		var targetElem = $(target);
		if (targetElem.length > 0) {
			//targetElem.append('<iframe id="iframe_' + pageId + '" style="height:100%;width:100%"></iframe>');
			//replaceIframeContent(document.getElementById("iframe_" + pageId), data);

			if (typeof $(currElem).attr("data-target-params") != "undefined") {
				var targetPageParams = $(currElem).attr("data-target-params");
				try {
					targetPageParams = beforePageParams(pageId, target, currElem, targetPageParams);
				}
				catch (ex) { }
				targetElem.attr("data-params", targetPageParams);
			}			
			targetElem.html(data);			
		}
	}

	displayLoader("hide");
}

function loadDataFromAPIs(pageId) {
	if (typeof pageId == 'undefined') {
		pageId = '';
	}
	else {
		pageId = "#" + pageId
	}

	$(pageId + " .axdata").each(function () {
		var datasource = $(this).attr("data-source");
		if (typeof datasource != "undefined") {
			loadData({
				dataId: datasource,
				pageId: pageId,
				elem: $(this),
				refresh: false,
				showLoader: true
			});
		}

	});

}

function loadData(dataOptions) { //dataId, pageId, elem, refresh
	var dataObj = new Object();
	dataObj.key = dataOptions.dataId;
	dataObj.refresh = dataOptions.refresh;

	dataObj.dataParams = new Object();

	if (dataOptions.showLoader == true) {
		console.log(dataOptions.dataId)
		displayLoader("show");
	}

	try {
		dataObj = beforeLoadData(dataObj, dataOptions.dataId, dataOptions.elem);
	}
	catch (ex) { }

	$.ajax({
		type: "POST",
		url: "../GetData?" + dataOptions.dataId,
		headers: {"Authorization": localStorage.getItem('RCP_JwtToken')},
		cache: false,
		contentType: "application/json;charset=utf-8",
		data: JSON.stringify(dataObj),
		async: true,
		dataType: "json",
		success: function (data) {			
			try{             
				data = data.result[0].result.row;
			}
			catch(ex){}

			loadData_Success(dataOptions.dataId, data, dataOptions.pageId, dataOptions.elem);
		},
		error: function (request, status, error) {
			displayLoader("hide");

			console.log(request);
			alert(request.responseText);
		}
	});	
}

function loadData_Success(dataId, data, pageId, elem) {
	if (typeof elem != "undefined") {
		if (data != "") {
			doRepeaterLoad(elem, data);
		}
	}
	try {
		dataObj = afterLoadData(data, dataId, elem);
	}
	catch (ex) { }

	displayLoader("hide");
}

function saveData(formId, isAsync) {
	var saveObj = new Object();
	saveObj.recordid = "0";
	saveObj.formId = formId;
	saveObj.userId = localStorage["AxpertConnectUser"];
	saveObj.isAsync = isAsync;
	saveObj = generateSaveJSON(saveObj, formId);


	//console.log(saveObj);
	//return;

	if (isAsync) {		
		callSaveDataToRMQ(formId, saveObj);
	}
	else {
		callSaveDataToDB(formId, saveObj);
	}
}

function generateSaveJSON(saveObj, formId) {

	if (typeof formId == 'undefined') {
		formId = '';
	}
	else {
		formId = "#" + formId
	}

	saveObj.saveJson = new Object();

	try {
		saveObj = beforeGenerateSaveJSON(formId, saveObj);
	}
	catch (ex) { };


	if (typeof saveObj.saveJson.skip == "undefined") {
		$(formId + ' .axsavefld').each(function () {
			var elem = $(this);
			saveObj.saveJson[elem.attr("data-target")] = getFieldValue(elem);
		});
	}

	try {
		saveObj = afterGenerateSaveJSON(formId, saveObj);
	}
	catch (ex) { };

	//saveObj.saveJson = saveJson;
	return saveObj;
}

function callSaveDataToDB(formId, saveObj) {

	$.ajax({
		type: "POST",
		url: "../SaveToDB",
		headers: {"Authorization": localStorage.getItem('RCP_JwtToken')},
		cache: false,
		contentType: "application/json;charset=utf-8",
		data: JSON.stringify(saveObj),
		async: true,
		dataType: "json",
		success: function (data) {
			saveData_Success(data, formId, saveObj);
		},
		error: function (request, status, error) {
			console.log(request);
			alert(request.responseText);
		}
	});	
}

function callSaveDataToRMQ(formId, saveObj) {

	$.ajax({
		type: "POST",
		url: "../SaveToRMQ",
		headers: { "Authorization": localStorage.getItem('RCP_JwtToken') },
		cache: false,
		contentType: "application/json;charset=utf-8",
		data: JSON.stringify(saveObj),
		async: true,
		dataType: "text",
		success: function (data) {
			//saveData_Success(data);
		},
		error: function (request, status, error) {
			console.log(request);
			alert(request.responseText);
		}
	});
}

function saveData_Success(data, formId, saveObj) {
	try {
		//alert("Result: " + data.result[0].message[0].msg + ". RecordID: " + data.result[0].message[0].recordid);
		console.log(data);
	}
	catch (ex) { };

	try {
		afterSaveSuccess(data, formId, saveObj);
	}
	catch (ex) { };
}

function dataConvert(type, data) {
	data = eval(data);
	switch (type) {
		case "Axpert-GetChoice":
			try {
				return data.result[0].result.row;
			}
			catch (ex) { };
		break;
	default:
		try {
			return data.result[0].result.row;
		}
		catch (ex) { };
	}
}

function getFieldValue(fld) {
	var fldVal = "";
	var fldType = fld.get(0).nodeName.toLowerCase();

	switch (fldType) {
		case "input":
			fldVal = fld.val();
			break;
		case "select":
			fldVal = fld.val();
			break;
		default:
			fldVal = fld.val();
	}	

	return fldVal;

}

//function getFieldValue(fld) {
//	var fldVal = "";
//	var fldType = fld.get(0).nodeName.toLowerCase();
//	var fldClass = '';

//	switch (fldType) {
//		case "input":
//			fldVal = fld.val();
//			break;
//		case "select":
//			if (fld.hasClass('js-select2') && fld.attr("multiple") == 'multiple') {

//			}
//			else
//				fldVal = fld.val();
//			break;
//		case "div":
//			if (fldClass == 'custom-select-2') {
//				fldVal = fld.find('input #' + value).prop('checked');
//			}
//	}

//	return fldVal;
//}

//function setFieldValue(fld, value) {
//	var fldClass = '';
//	if (fldClass == 'opcalendar') {
//		$('#opcalLabel').text(value);
//	}

//	if (fldClass == 'custom-select-2') {
//		fld.find('input #' + value).prop('checked');
//	}
//	return true;
//}

var toBase64 = file => new Promise((resolve, reject) => {
	const reader = new FileReader();
	reader.readAsDataURL(file);
	reader.onload = () => resolve(reader.result.split(',')[1]);
	reader.onerror = error => reject(error);
});

async function uploadFile(file, filePath) {
	file = document.querySelector("#" + file).files[0];
	var fileBase64 = await toBase64(file);
	var fileDef = new Object();
	fileDef.fileName = file.name;
	fileDef.fileBase64 = fileBase64;
	fileDef.filePath = filePath;

	$.ajax({
		type: "POST",
		url: "../UploadFile",
		headers: { "Authorization": localStorage.getItem('RCP_JwtToken') },
		cache: false,
		contentType: "application/json;charset=utf-8",
		data: JSON.stringify(fileDef),
		async: true,
		dataType: "text",
		success: function (data) {
			uploadFile_Success(data);
		},
		error: function (request, status, error) {
			console.log(request);
			alert(request.responseText);
		}
	});
}

function downloadFile(file, filePath) {
	var fileDef = new Object();
	fileDef.fileName = file.name;
	fileDef.filePath = filePath;

	$.ajax({
		type: "POST",
		url: "../DownloadFile",
		headers: { "Authorization": localStorage.getItem('RCP_JwtToken') },
		cache: false,
		contentType: "application/json;charset=utf-8",
		data: JSON.stringify(fileDef),
		async: true,
		dataType: "text",
		success: function (data) {
			uploadFile_Success(data);
		},
		error: function (request, status, error) {
			console.log(request);
			alert(request.responseText);
		}
	});
}

function uploadFile_Success(data) {
	//alert(data);
}

Date.prototype.toDateFormat = function (format) {
	if (format.toLowerCase() == "dd-mmm-yyyy") {
		let monthNames = ["Jan", "Feb", "Mar", "Apr",
			"May", "Jun", "Jul", "Aug",
			"Sep", "Oct", "Nov", "Dec"];

		let day = this.getDate();

		let monthIndex = this.getMonth();
		let monthName = monthNames[monthIndex];

		let year = this.getFullYear();

		return `${day}-${monthName}-${year}`;
	}
}

function copyHTML(sourceId, sourceHtml, target) {
	if (sourceHtml.indexOf('axrownum') > -1) {
		if (typeof rownumList.sourceId != "undefined") {
			sourceHtml = sourceHtml.replaceAll("axrownum", rownumList.sourceId.toString());
			sourceHtml = sourceHtml.replaceAll("axhidden", "");
		}
		else {
			rownumList.sourceId = 1;
			sourceHtml = sourceHtml.replaceAll("axrownum", rownumList.sourceId.toString());
			sourceHtml = sourceHtml.replaceAll("axhidden", "");
		}
	}

	$(target).append(sourceHtml);

	try {		
		afterCopyHTML(sourceId, sourceHtml, target, rownumList.sourceId);
	}
	catch (ex) { };

	rownumList.sourceId++;
}

function logout() {
	localStorage.removeItem("RCP_JwtToken");
	window.location.href = "login";
}

function setMobileViewFlag() {
	if ($(window).width() < 450) {
		localStorage.setItem("RCP_MobileView", "true");
	}
	else {
		localStorage.setItem("RCP_MobileView", "false");
	}
}

function displayLoader(option) {
	if (option == "show") {
		apiCallsCount++;
		if ($("body .bgloader:not(.mainloader)").length == 0) {
			$("body").append('<div class="d-flex justify-content-center bgloader" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%; opacity: 1; background-color: #e1e1e1;z-index:99999;"><div class="spinner-border text-danger" style="margin-top: 30%;" role="status"><span class="visually-hidden"></span></div></div>');
		}		
	}
	else if (option == "hide") {
		apiCallsCount--;
		if (apiCallsCount <= 0) {
			$("body .bgloader:not(.mainloader)").fadeOut(100, function () {
				$("body .bgloader:not(.mainloader)").remove();
			});
		}
	}
}

function displayMainLoader(option) {
	if (option == "show") {
		$("#divHome").parent().append('<div class="d-flex justify-content-center bgloader mainloader" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%; opacity: 1; background-color: #e1e1e1;z-index:99999;"><div class="spinner-border text-danger" style="margin-top: 30%;" role="status"><span class="visually-hidden"></span></div></div>');
	}
	else if (option == "hide") {
		$("body .bgloader.mainloader").fadeOut(300, function () {
			$("body .bgloader.mainloader").remove();
		});
	}
}

function addPopupsHTML() {
	$('#confirmPopup').removeData();
	$('#messagePopup').removeData();

	$("#divHome").parent().find("#confirmPopup").remove();
	$("#divHome").parent().append(`<div class="modal fade" id="confirmPopup" tabindex="-1" role="dialog"
                    aria-labelledby="exampleModalCenterTitle" aria-hidden="true">
                    <div class="modal-dialog modal-dialog-centered" role="document">
                        <div class="modal-content rounded-button border-0">
                            <div class="modal-header  primaryBgColor text-light p-1 px-2">
                                <h5 class="modal-title ms-3" id="confirmPopupTitle">Confirmation Title</h5>
                                <button type="button" class="border-0 bg-transparent" class="close" data-bs-dismiss="modal"
                                    aria-label="Close">
                                    <span class="btn btn-danger btn-sm cancelBorder  px-2 py-1" aria-hidden="true">&times;</span>
                                </button>
                            </div>
                            <div class="modal-body">
                                <p class="mb-0" id="confirmPopupMsg">Confirmation Message</p>
                            </div>
                            <div class="modal-footer justify-content-center p-1">
                                <button type="button" data-bs-dismiss="modal" class="btn btn-sm btn-grayModal" id="confirmPopupYes" data-confirm-id="" onclick="return doConfirmYes();">Proceed</button>
                                <button type="button" data-bs-dismiss="modal" class="btn btn-sm btn-grayModal" id="confirmPopupNo">Cancel</button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>`);

	$("#confirmPopup").off('hidden.bs.modal').on('hidden.bs.modal', function (e) {
		$("#confirmPopup #confirmPopupTitle").html("Confirmation Title");
		$("#confirmPopup #confirmPopupMsg").html("Confirmation Message");
		$("#confirmPopup #confirmPopupYes").html("Proceed");
		$("#confirmPopup #confirmPopupNo").html("Cancel");
		$("#confirmPopup #confirmPopupYes").attr("data-confirm-id", "");
		$("#confirmPopup #confirmPopupYes").unbind('click');
		$("#confirmPopup #confirmPopupNo").unbind('click');
	})

	$("#divHome").parent().find("#messagePopup").remove();
	$("#divHome").parent().append(`<div class="modal fade" id="messagePopup" tabindex="-1" role="dialog"
                    aria-labelledby="exampleModalCenterTitle" aria-hidden="true">
                    <div class="modal-dialog modal-dialog-centered" role="document">
                        <div class="modal-content rounded-button border-0">
                            <div class="modal-header  primaryBgColor text-light p-1 px-2">
                                <h5 class="modal-title ms-3" id="messagePopupTitle">Message Title</h5>
                                <button type="button" class="border-0 bg-transparent" class="close" data-bs-dismiss="modal"
                                    aria-label="Close">
                                    <span class="btn btn-danger btn-sm cancelBorder  px-2 py-1" aria-hidden="true">&times;</span>
                                </button>
                            </div>
                            <div class="modal-body">
                                <p class="mb-0" id="messagePopupMsg">Message</p>
                            </div>
                            <div class="modal-footer justify-content-center p-1">
                                <button type="button"  data-bs-dismiss="modal" class="btn btn-sm btn-grayModal" id="messagePopupOk">Ok</button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>`);

	$("#messagePopup").on('hidden.bs.modal', function (e) {
		$("#messagePopup #messagePopupTitle").html("Message Title");
		$("#messagePopup #messagePopupMsg").html("Message");
		$("#messagePopup").off('hide.bs.modal').off('show.bs.modal');
		$("#messagePopup #messagePopupOk").unbind('click');
	})
}

function confirmAlert(confirmOptions) {
	if (confirmOptions.hide == true) {
		$("#confirmPopup").modal("hide");		
	}
	else {
		if (typeof confirmOptions.title != "undefined") $("#confirmPopup #confirmPopupTitle").html(confirmOptions.title);
		if (typeof confirmOptions.message != "undefined") $("#confirmPopup #confirmPopupMsg").html(confirmOptions.message);
		if (typeof confirmOptions.yesCaption != "undefined") $("#confirmPopup #confirmPopupYes").html(confirmOptions.yesCaption);
		if (typeof confirmOptions.noCaption != "undefined") $("#confirmPopup #confirmPopupNo").html(confirmOptions.noCaption);
		if (typeof confirmOptions.id != "undefined") $("#confirmPopup #confirmPopupYes").attr("data-confirm-id", confirmOptions.id);

		if (typeof confirmOptions.onload != "undefined") {
			$("#confirmPopup").on('show.bs.modal', confirmOptions.onload);
		}

		if (typeof confirmOptions.onclose != "undefined") {
			$("#confirmPopup").on('hide.bs.modal', confirmOptions.onclose);
		};

		if (typeof confirmOptions.yesClick != "undefined") {
			$("#confirmPopup #confirmPopupYes").unbind('click').click(confirmOptions.yesClick);
		}

		if (typeof confirmOptions.noClick != "undefined") {
			$("#confirmPopup #confirmPopupNo").unbind('click').click(confirmOptions.noClick);
		};

		$("#confirmPopup").modal("show");
	}
}

function messageAlert(messageOptions) {
	if (messageOptions.hide == true) {
		$("#messagePopup").modal("hide");
	}
	else {
		if (typeof messageOptions.title != "undefined") $("#messagePopup #messagePopupTitle").html(messageOptions.title);
		if (typeof messageOptions.message != "undefined") $("#messagePopup #messagePopupMsg").html(messageOptions.message);
		
		if (typeof messageOptions.onload != "undefined") {
			$("#messagePopup").on('show.bs.modal', messageOptions.onload);
		}

		if (typeof messageOptions.onclose != "undefined") {
			$("#messagePopup").on('hide.bs.modal', messageOptions.onclose);
		};

		if (typeof messageOptions.okClick != "undefined") {
			$("#messagePopup #messagePopupOk").unbind('click').click(messageOptions.okClick);
		}		

		$("#messagePopup").modal("show");
	}
}
