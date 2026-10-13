$(document).ready(function () {
	
	var dsInputJsonEditor = CodeMirror.fromTextArea(document.getElementById('DataSourceFormat'), {
        height: "350px",
        lineNumbers: true,
        matchBrackets: true,
        autoCloseBrackets: true,
        mode: "application/ld+json",
        lineWrapping: true,
        theme: "material"
    });

    var dsSuccessJsonEditor = CodeMirror.fromTextArea(document.getElementById('DataSyncInitFormat'), {
        height: "350px",
        lineNumbers: true,
        matchBrackets: true,
        autoCloseBrackets: true,
        mode: "application/ld+json",
        lineWrapping: true,
        theme: "material"
    });

    dsInputJsonEditor.on('change', function (cMirror) {
        // get value right from instance
        document.getElementById('DataSourceFormat').value = cMirror.getValue();
    });

    dsSuccessJsonEditor.on('change', function (cMirror) {
        // get value right from instance
        document.getElementById('DataSyncInitFormat').value = cMirror.getValue();
    });

	$("#ddlUserGroups").select2({
		multiple: true,
		closeOnSelect: false,
		placeholder: "Please select the user groups...",
		allowHtml: true,
		allowClear: true,
		tags: true
	});
	
	$("#ddlDataSources").select2({
		multiple: true,
		closeOnSelect: false,
		placeholder: "Please select the user groups...",
		allowHtml: true,
		allowClear: true,
		tags: true
	});
	
	$("#chkAllUserGroups").click(function(){
        if($("#chkAllUserGroups").is(':checked')){
            $("#ddlUserGroups > option").prop("selected", "selected");
            $("#ddlUserGroups").trigger("change");
        } else {
            $("#ddlUserGroups").val('').trigger("change");
        }
    });
});