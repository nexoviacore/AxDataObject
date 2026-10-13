var sendDocParams = {};
$(document).ready(function () {
    if (typeof window.parent.pageParams.patientDetails != "undefined") {
        sendDocParams = Object.assign({}, window.parent.pageParams.patientDetails);
    }
    else {
        sendDocParams = Object.assign({}, window.parent.pageParams.docParams);
    }

    $("#sendTarget").val(sendDocParams.mobileno);

    $('input[type=radio][name=modeOfSend]').off("change").change(function () {
        if (this.value == 'whatsapp') {
            $("#sendTarget").val(sendDocParams.mobileno);
        }
        else if (this.value == 'email') {
            $("#sendTarget").val(sendDocParams.email.trim());
        }
    });
});

function generateSaveDataOptions(saveDataOptions) {
    if (saveDataOptions.formId == "op_senddocument") {
        saveDataOptions.saveJson = function () {
            var saveJson = {};
            var uploadTime = new Date().toLocaleString().replaceAll("/", "_").replaceAll(":", "_")
                .replaceAll(" ", "_").replaceAll(",", "_") + "_";

            saveJson.mobile = $("#sendTarget").val();
            saveJson.uhid = sendDocParams.uhid;
            saveJson.appno = sendDocParams.docno;
            saveJson.opno = sendDocParams.opno;
            saveJson.upload_type = $("input[name=modeOfSend]:checked").val();

            var sendOptions = [];
            $(".sendOptions:checked").each(function () {
                sendOptions.push($(this).val());
            });

            var sendFiles = [];
            sendOptions.forEach(function (item, index, arr) {
                $("#sendDocumentList div.sendFiles[data-filetype='" + item + "']").each(
                    function () {
                        sendFiles.push($(this));
                    })
            });

            var rowno = 1;
            var dc2JsonArr = new Array();

            sendFiles.forEach(function (item, index, arr) {
                var elem = $(this);
                var dc2Json = new Object();
                dc2Json.rowno = rowno;
                dc2Json.text = 0;

                dc2Json.columns = {
                    "recordurl": item.data("fileurl"),
                    "filename": item.data("filename"),
                    "documenttype": item.data("filetype")
                }
                dc2JsonArr.push(dc2Json);
                rowno++;
            });
            saveJson.dc2JsonArr = dc2JsonArr;
            return saveJson;
        }

        saveDataOptions.afterSave = function () {
            messageAlert({
                title: "Success Message",
                message: "Document sent.",
                onclose: function () { window.parent.$("#sendDocument").modal("hide"); }
            })
            
        };
    }

    return saveDataOptions;
}

function generateLoadDataOptions(dataOptions) {
    if (dataOptions.dataId == "op_doctor_docs") {
        dataOptions.beforeLoad = function (dataOptions) {
            dataOptions.dataObj.dataParams = new Object();
            dataOptions.dataObj.dataParams.opno = sendDocParams.opno;
            dataOptions.dataObj.dataParams.uhid = sendDocParams.uhid;

            return dataOptions;
        };
        dataOptions.afterLoad = function (data, dataOptions) {
            if (data == "" || data.length == 0) {
                dataOptions.elem.find('tr').html("No documents available")
            }
        }
    }

    return dataOptions;
}
function senddocwhatsappemail() {
    if ($('#sendTarget').val().trim() == '') {
        ToastMaker('Whatsapp number / Email id mandatory');
        return false;
    }

    if ($('input[name="doccheckbox"]:checked').length == '0') {
        ToastMaker('Documents are mandatory');
        return false;
    }
    $('#sendPatientConfirmation').modal('show');
}