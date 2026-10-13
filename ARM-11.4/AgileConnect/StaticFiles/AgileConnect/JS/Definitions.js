$(document).ready(function () {
	
	var defHtmlEditor = CodeMirror.fromTextArea(document.getElementById('DefinitionHTML'), {
		height: "350px",
		lineNumbers: true,
		mode: "htmlmixed",
		theme: "material"
	});

	defHtmlEditor.on('change', function (cMirror) {
		// get value right from instance
		document.getElementById('DefinitionHTML').value = cMirror.getValue();
	});

	$("#ddlUserGroups").select2({
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