var uploadDocParams = {};
$(document).ready(function () {
    if (typeof window.parent.pageParams.patientDetails != "undefined") {
        uploadDocParams = Object.assign({}, window.parent.pageParams.patientDetails);
    }
    else {
        uploadDocParams = Object.assign({}, window.parent.pageParams.docParams);
    }

    var $group = $('.input-group');
    var $file = $group.find('input[type="file"]')
    var $browse = $group.find('[data-action="browse"]');
    var $fileDisplay = $group.find('[data-action="display"]');
    var $reset = $group.find('[data-action="reset"]');
    var resetHandler = function (e) {
        if ($file.length === 0) {
            return;
        }
        $file[0].value = '';
        if (!/safari/i.test(navigator.userAgent)) {
            $file[0].type = '';
            $file[0].type = 'file';
        }
        $file.trigger('change');
    };
    var browseHandler = function (e) {
        //If you select file A and before submitting you edit file A and reselect it it will not get the latest version, that is why we  might need to reset.
        //resetHandler(e);
        $file.trigger('click');

    };
    $browse.on('click', function (e) {
        if (event.which != 1) {
            return;
        }
        browseHandler();
    });
    $fileDisplay.on('click', function (e) {
        if (event.which != 1) {
            return;
        }
        browseHandler();
    });
    $reset.on('click', function (e) {
        if (event.which != 1) {
            return;
        }
        resetHandler();
    });

    $file.on('change', function (e) {
        var files = [];
        if (typeof e.currentTarget.files) {
            for (var i = 0; i < e.currentTarget.files.length; i++) {
                files.push(e.currentTarget.files[i].name.split('\\/').pop())
            }
        } else {
            files.push($(e.currentTarget).val().split('\\/').pop())
        }
        $fileDisplay.val(files.join('; '))
    });

    $(".docTypeSelect").selectpicker();
})

function generateSaveDataOptions(saveDataOptions) {
    if (saveDataOptions.formId == "op_uploaddocument") {
        saveDataOptions.saveJson = function () {
            var saveJson = {};
            var uploadTime = new Date().toLocaleString().replaceAll("/", "_").replaceAll(":", "_")
                .replaceAll(" ", "_").replaceAll(",", "_") + "_";

            saveJson.mobile = "0";
            saveJson.uhid = uploadDocParams.uhid;
            saveJson.appno = uploadDocParams.appno;
            saveJson.opno = uploadDocParams.opno;
            saveJson.upload_type = "Doctor";

            saveJson.filename = uploadDocParams.opno + "_" + document.querySelector("#uploadFile").files[0].name;
            saveJson.filepath = uploadDocParams.uhid + "/" + $(".docTypeSelect ").find("option:selected").val().toString();
            saveJson.documenttype = $(".docTypeSelect ").find("option:selected").val().toString();
            return saveJson;
        }

        saveDataOptions.afterSave = function () {
            messageAlert({
                title: "Success Message",
                message: "Document uploaded.",
                onclose: function () { window.parent.$("#uploadDoc").modal("hide"); }
            })
        };
    }

    return saveDataOptions;
}

function generateFileUploadOptions(fileUploadOptions) {
    fileUploadOptions.fileName = uploadDocParams.opno + "_" + fileUploadOptions.file.name;
    fileUploadOptions.filePath = uploadDocParams.uhid + "/" + $(".docTypeSelect ").find("option:selected").val().toString();
    return fileUploadOptions;
}

function opUploadFile() {
    $("#opUploadFile1").click();
    $("#opUploadFile2").click();
}