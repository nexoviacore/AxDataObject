$(document).ready(function () {
	
	var defFormat = CodeMirror.fromTextArea(document.getElementById('ExternalAuthRequest'), {
        height: "350px",
        lineNumbers: true,
        matchBrackets: true,
        autoCloseBrackets: true,
        mode: "application/ld+json",
        lineWrapping: true,
        theme: "material"
    });

    defFormat.on('change', function (cMirror) {
        // get value right from instance
        document.getElementById('ExternalAuthRequest').value = cMirror.getValue();
    });

    var defResponse = CodeMirror.fromTextArea(document.getElementById('ExternalAuthResponse'), {
        height: "350px",
        lineNumbers: true,
        matchBrackets: true,
        autoCloseBrackets: true,
        mode: "application/ld+json",
        lineWrapping: true,
        theme: "material"
    });

    defResponse.on('change', function (cMirror) {
        // get value right from instance
        document.getElementById('ExternalAuthResponse').value = cMirror.getValue();
    });
	
	$("#ddlHTMLDefinitions").select2({
		multiple: true,
		closeOnSelect: false,
		placeholder: "Please select the HTML Definitions...",
		allowHtml: true,
		allowClear: true,
		tags: true
	});
	
	$("#chkAllHTMLDefinitions").click(function(){
        if($("#chkAllHTMLDefinitions").is(':checked')){
            $("#ddlHTMLDefinitions > option").prop("selected", "selected");
            $("#ddlHTMLDefinitions").trigger("change");
        } else {
            $("#ddlHTMLDefinitions").val('').trigger("change");
        }
    });
	

	$("#ddlDataSources").select2({
		multiple: true,
		closeOnSelect: false,
		placeholder: "Please select the DataSources...",
		allowHtml: true,
		allowClear: true,
		tags: true
	});
	
	$("#chkAllDataSources").click(function(){
        if($("#chkAllDataSources").is(':checked')){
            $("#ddlDataSources > option").prop("selected", "selected");
            $("#ddlDataSources").trigger("change");
        } else {
            $("#ddlDataSources").val('').trigger("change");
        }
    });
});