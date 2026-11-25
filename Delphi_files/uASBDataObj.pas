unit uASBDataObj;
{copied from \Axpert9-XE3\Ver 11.2}
{copied from \\192.168.2.6\Axpert9-XE3\Ver 11.3\AsbCommonObjs}
{copied from \\192.168.2.6\Axpert9-XE3\Ver 11.4\AsbObjs}
interface

uses  Classes , XMLDoc, XMLIntf , Variants , uDBManager , uConnect , uAxProvider ,
      uXds, DateUtils , uDbCall , uStructDef , uDoCoreAction , db , uFillStruct , uASBCommonObj,
      uStoreData,uProfitEval, Windows,IdGlobal, AXMLibrary,uGetDependencies, System.StrUtils, SysUtils,
      {$IFDEF VER240} DBXJSON {XE3} {$ELSE} JSON {Above XE3} {$ENDIF};

type
  TASBDataObj = class
  private
    dbresave : TDbCall;
    TransAccess,atnode,wfnode,DataFilledDcs,dbTransid,DSignStatus,ivParamList  : String;
    msgnode_created,IsImpFromCSVToDC : boolean;
    SaveNodeFromScripts : AnsiString;

    function ConvertToCondStr(fldname, dtype, op, fldvalue,
      fldvalue1: String): String;
    function ConvertToExpr(fldname, dtype, op, fldvalue,
      fldvalue1: String): String;
    function GetTransControl(exprlist: TStringList; exprstr: String): String;
    function GetTransControlString(cstr, tname: String;
      accxml: ixmldocument): String;
    function FrameCondStr(condlist, condfldlist: TStringList;
      tname: String): String;
    procedure CreateAttachmentNode;
    procedure CreateFormControlNode;
    function SaveFromAction: String;
    function LoadDataInAction(s: String; n: ixmlNode): String;
    function LoadDataForAction(transid: String; hypernode: ixmlNode): String;
    function OpenDataInAction(s: String; rnode: ixmlNode): String;
    function PageDataInAction(s: String; rnode: ixmlnode): String;
    procedure SetValueInAction(n: IXMLNode);
    procedure DoFillInAction(n: IXMLNode);
    function CancelDataInAction: String;
    function DeleteDataInAction: String;
    function PostIviewFromAction(n: ixmlnode): String;
    procedure CopyCSVToDCInAction(Filename, transname, hline, seperator: String;
      Csflist: TStringList);
    function GetDataSimple(OneLine, seperator: String; k: integer): String;
    procedure RegisterActiveRow(fno, rno: integer);
    function LoadXMLDataFromWS(xml: WideString): IXMLDocument;
    procedure DeleteRowsFromStoreData(delrowsnode: ixmlnode);
    procedure ChangedWebRowsList(delrowsnode: ixmlnode);
    procedure UpdateChangedRowsList(fno, rno: integer);
    procedure CreateMemVarNode;
    function GetMemVars(parser: TProfitEval): String;
    procedure LoadCDSAndCreateJSON(fm: pFrm;mustloadCDS : TXDS);
    function GetMdMapFields(totfldlist: String): String;
    procedure PopGrrid_GetAxpRecid(frameno, rowno: integer; recid: extended);
    function GetDCCaption(dccaption: string): string;
    function CheckFileExistence(TransId, DocName: String): String;
    function DeleteExistence(TransId, DocName: String): Boolean;
    function IsForIviewDef(x: IXMLDocument): Boolean;
    function SetValueForScript(FieldValues: String): String;
    function CancelDataForScript(trnsid,rcdid,rem: String): String;
	  function CancelDataForAction(rem: String): String;
    function LoadDataInScript(s,pFieldValues: String): String;
    function LoadDataForScript(transid, pFieldValues: String): String;
    function GetIviewParams(ivnm, sqltxt: String): String;
    function DeleteDataInScript(trnsid, rcdid, showresult: String): String;
    procedure ProcessandPopulateDeletedRows(delrowsNode: IXMLNode);
//    procedure RegisterConfigData(s: string);
    //procedure GetDataNode;

  public
    dbm : TDBManager;
    AxProvider : TAxProvider;
    connection : pConnection;
    DbCall : TDbCall;
    act :TDoCoreAction;
    ASBCommonObj : TASBCommonObj ;
    jsonstr, actStructType,VisibleDCs : String;
    XMLDoc : IXMLDocument;
    FillGridExecuted,MakeMemVarNode,FormLoadingAct,depCall : boolean;
    ChangeDCCap : Boolean;//Dynamic Dc Caption
    ServiceName, AxmAttachMode, AxmAttachDir : string;
    FastDataFlag , DataSubmitSort  : Boolean;
    act_fgname, FormLoadFldList : string;
    fg_record_count , activerow , existing_gridrows : integer;
    LoadDataByAction , RefreshGrid , imagefromdb , FromScripts : boolean;

    procedure GridDependentsToJSON(GridDependents: String);  overload ;
    procedure GridDependentsToJSON(GridDependents: String;fm:pFrm); overload ;
    procedure GridDependentsToJSON(GridDependents: String; depflds : TStringList); overload ;
    function PopGridToJSON(PopGrid:pPopGrid; RowNo:Integer; WithDropDown: Boolean):integer;
    procedure GetAxpRecid(FrameNo, RowNo:integer);overload;
    procedure GetAxpRecid(FrameNo:integer);overload;
    procedure GetAxpRecid(fm: pFrm); overload;
    function GridToJSON(FrameNo: integer; WithDropDown:boolean; OnlyChanged:Boolean=false;actrow:integer=-1): integer;
    procedure EndDataJSON;
    procedure DCToJSON(DCNumber, RowCount:Integer); overload ;
    procedure DCToJSON(DCNumber, RowCount, ActRow :Integer); overload;
    procedure DCToJSON(fm: pFrm ; rowcount:integer); overload;
    procedure PopDCToJSON(dcnumber, rowcount: integer);
    function LOadDCToJSON(FrameNo: integer): integer;
    procedure FieldToJSON(fd:pfld; frec:pFieldRec;rno:integer=-1);
    function DropDownToJSON(fd: pfld; var dpm: String): String;
    function GetJSON: String;
    procedure CreateAndSetDbCall(transid: String ; sxml : widestring ; StructXml : IXMLDocument = nil); overload;
    procedure CreateAndSetDbCall(act,transid,fldlist,visibleDCs,fglist,fillgriddc: String ; sxml : IXMLDocument); overload;
    procedure CreateAndSetDbCallForAPI(transid: String; sxml: widestring; StructXml: IXMLDocument = nil);
    procedure CreateActionObj(transid , stype : String);
    function CheckForAccess(sname: String) : String;
    procedure SubmitClientValuesToSD;
    procedure RemoveDeletedWebRowsFromStoreDate;
    procedure CreateChangedRowsList;
    procedure GetDepentendFldList(fname: String; flist: TStringList);
    procedure CreateCommandNode;
    procedure CreateMessageNode;
    procedure RegisterGlobalVars;
    procedure ImageFieldToJSON(fd: pfld;SrcImgDet:String=''; frec:pFieldRec=nil;rowstring:string='0');
    function LoadSubGridToJSON(FrameNo: integer): integer;
    procedure DummyDCNodeToJSON(dcnumber, rowcount: integer;s:string);
    function GetMustLoadFields: String;
    procedure GetMustLoadFieldValues(mustloadflds,recid : String);
    procedure RefreshDC(FrmNo: Integer);
    procedure ChangeHasDataRowStatus(fm : pFrm);
    function LoadAndSave(pTransid, pRecordId: String): String;
    procedure CreateWorkflowNode;
    function DummyDynamicDCNodeToJSON(fm : pFrm ; rowcount: integer; s: string):string;
    function UpdateDigiSign(TransId, PrintDocName, FileName : String): String;
    function getusermailid(username: String): String;
    procedure SubmitClientValuesToSD_New;
    function GridToJSONNew(FrameNo: integer; WithDropDown:boolean; OnlyChanged:Boolean=false): integer;
    procedure FieldToJSONNew(fd: pfld; frec: String;rno:integer=-1);
    procedure LoadDataFieldToJSON(fd: pfld; frec: pFieldRec);
    function LoadDCToJSONNew(FrameNo: integer): integer;
    procedure QuickChangeHasDataRowStatus(fm: pFrm);
    procedure SubmitClientValuesToSDForPEG;
    procedure SubmitClientValuesToSD_ForKeyValueSave;
    procedure SubmitClientValuesToSD_ForLoadRec;

    constructor Create;
    destructor Destroy; override;
  end ;

implementation

{ TASBDataObj }

//uses uASBScriptObj;

constructor TASBDataObj.Create;
begin
  MakeMemVarNode := true;
  jsonstr := '';
  TransAccess := '';
  FillGridExecuted := false;
  msgnode_created := false;
  changedccap := False;//Dynamic Dc Caption
  FastDataFlag := false;
  FormLoadingAct := false;
  dbcall := nil;
  act := nil;
  DataSubmitSort := true;
  depCall := False;
  act_fgname := '';
  fg_record_count := -1;
  activerow := -1;
  LoadDataByAction := False;
  RefreshGrid := False;
  FormLoadFldList := '';
  imagefromdb := false;
  IsImpFromCSVToDC := False;
  SaveNodeFromScripts := '';
  FromScripts := false;
  existing_gridrows := 0;
end;

destructor TASBDataObj.Destroy;
begin
  dbm.gf.dodebug.msg('Freeing DbCall Resources');
  if assigned(dbcall) then
  begin
     dbcall.destroy;
     dbcall := nil;
  end;
  dbm.gf.dodebug.msg('Freeing Action Obj Resources');
  if assigned(act) then
  begin
     act.destroy;
     act := nil;
  end;
  inherited;
end;

procedure TASBDataObj.CreateAndSetDbCall(transid : String ; sxml : widestring ; StructXml:IXMLDocument=nil);
begin
    dbm.gf.dodebug.msg('Creating DbCall');
    dbcall := TDbCall.Create;
    dbcall.Dbm := dbm;
    dbcall.axprovider := axprovider;
    if StructXml <> nil then
       dbcall.sxml := StructXml
    else if sxml <> '' then
       dbcall.sxml := LoadXMLDataFromWS(sxml)
    else
    begin
      dbm.gf.isSXMLGetFromClient := false;
      dbm.gf.dodebug.msg('Structure xml not received from client');
    end;
    dbm.gf.FastDataFlag := FastDataFlag;
    dbcall.transid := transid;
    dbcall.Validate.FastDataFlag := FastDataFlag;
end;

procedure TASBDataObj.CreateAndSetDbCallForAPI(transid : String ; sxml : widestring ; StructXml:IXMLDocument=nil);
  var deps : TGetDependencies;
begin
    dbm.gf.dodebug.msg('Creating DbCall');
    dbcall := TDbCall.Create;
    dbcall.Dbm := dbm;
    dbcall.axprovider := axprovider;
    if StructXml <> nil then
       dbcall.sxml := StructXml
    else if sxml <> '' then
       dbcall.sxml := LoadXMLDataFromWS(sxml)
    else begin
      dbcall.sxml :=  axprovider.GetStructure('tstructs',transid,'','');
      if trim(dbcall.sxml.XML.Text) = '<root/>' then raise Exception.create('TStructure ['+transid+'] not available...');
      deps := TGetDependencies.Create(axprovider,transid,dbcall.sxml);
      deps.GetDependencies;
      deps.Destroy;
    end;
    dbm.gf.FastDataFlag := FastDataFlag;
    dbcall.transid := transid;
    dbcall.Validate.FastDataFlag := FastDataFlag;
end;

procedure TASBDataObj.CreateAndSetDbCall(act,transid,fldlist,visibleDCs,fglist,fillgriddc : String ; sxml : IXMLDocument);
begin
    dbm.gf.dodebug.msg('Creating DbCall');
    dbcall := TDbCall.Create;
    dbcall.Dbm := dbm;
    dbcall.axprovider := axprovider;
    dbcall.sxml := sxml;
    dbcall.CreateObjects(act,transid,fldlist,visibleDCs,fglist,fillgriddc);
    dbcall.Validate.FastDataFlag := FastDataFlag;
end;

procedure TASBDataObj.CreateActionObj(transid , stype: String);
begin
    dbm.gf.dodebug.msg('Creating Action object');
    act := TDoCoreAction.Create(axprovider) ;
    act.coreparser.axp:=axprovider;
    act.stype := stype;
    act.sname := transid ;
    act.iviewtable := '' ;
    act.transid := transid ;
    act.MainXML := XMLDoc;
    act.SaveFromAction := SaveFromAction;
    act.LoadDataInAction := LoadDataInAction;
    act.OpenDataInAction := OpenDataInAction;
    act.PageDataInAction := PageDataInAction;
    act.SetValueInAction := SetValueInAction;
    act.DoFillInAction := DoFillInAction;
    act.DeleteDataInAction := DeleteDataInAction;
    act.CancelDataInAction := CancelDataInAction;
    act.PostIviewFromAction := PostIviewFromAction;
    act.CopyCSVToDCInAction := CopyCSVToDCInAction;
    act.SetValueForScript := SetValueForScript;
    act.CancelDataForScript := CancelDataForScript;
    act.CancelDataForAction := CancelDataForAction;
    act.LoadDataInScript := LoadDataInScript;
    act.DeleteDataInScript := DeleteDataInScript;
    if assigned(dbcall) then
    begin
      act.MainValidate := dbcall.Validate;
      act.StoreData:=dbcall.StoreData;
    end;
end;

procedure TASBDataObj.SubmitClientValuesToSD;
var j,fno,rno,rowno,LastRowNo,LastFrameNo,pfno,prno:integer;
    f,v,dt,srecid, ov,s,encrypt_val:String;
    n,rnode : ixmlnode;
    id,oldid,recid : extended;
    fld : pFld;
begin
  dbcall.StoreData.webdata := true;
  dbcall.storedata.ClientNotify:=false;
  dbm.gf.dodebug.msg('Submiting & Registering data ');
  RegisterGlobalVars;
  fno:=0;rno:=0;
  s:=vartostr(xmldoc.DocumentElement.Attributes['frameno']);
  if s<>'' then fno := strtoint(s);
  s:=vartostr(xmldoc.DocumentElement.Attributes['activerow']);
  if s<>'' then rno := strtoint(s);
  if vartype(xmldoc.DocumentElement.Attributes['pdc']) <> varnull then
  begin
     if vartostr(xmldoc.DocumentElement.Attributes['pdc']) <> '' then
        pfno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['pdc']));
  end;
  if vartype(xmldoc.DocumentElement.Attributes['prow']) <> varnull then
  begin
     if vartostr(xmldoc.DocumentElement.Attributes['prow']) <> '' then
        prno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['prow']));
  end;
  rnode := xmldoc.DocumentElement.ChildNodes.FindNode('data'); // his node used for savedata service
  if rnode = nil then rnode := xmldoc.DocumentElement.ChildNodes.FindNode('FieldList'); // This node used for dofillgridvalues service
  if rnode = nil then n := xmldoc.DocumentElement.ChildNodes.FindNode('varlist'); // his node used for action service
  if assigned(n) then rnode := n.ChildNodes[0];
  if rnode = nil then rnode := xmldoc.DocumentElement;
  id := 0; oldid := 0; recid := 0;LastRowNo:=-1; LastFrameNo := -1;
  for j:=0 to rnode.childnodes.count-1 do begin
    n := rnode.childnodes[j];
    f:=vartostr(n.NodeName);
    v:=vartostr(n.NodeValue);
    //This section reads the axp_recid field from XML into recid variable. This is used as recordid for all subsequent elements.
    if copy(f,1,9) = 'axp_recid' then  begin
       recid := dbm.gf.strtofloatz(v);
       srecid := v;
       continue;
    end;
    //Convert special characters to actual characters.

    if pos('<br>',v) > 0  then v := dbm.gf.FindAndReplace(v,'<br>',#$D#$A);
    ov:='';oldid:=0; encrypt_val := '';
    fld:=dbcall.struct.GetField(f);
    if assigned(fld) then
    begin
      //submit to storedata.
      id := dbm.gf.strtofloatz(vartostr(n.attributes['id']));
      oldid := dbm.gf.strtofloatz(vartostr(n.attributes['oldid']));
      dt := fld.DataType;
      if vartostr(n.Attributes['rowno']) <> '' then
         rowno := strtoint(vartostr(n.Attributes['rowno']))
      else rowno := 1;
      if rowno = 0 then rowno := 1;
      ov:=vartostr(n.attributes['ov']);
      if vartostr(n.Attributes['multiselect']) = 'true' then
      begin
        fld.cl_multiselect := true;
        if vartostr(n.Attributes['separator']) = '' then
           fld.Separator := ','
        else
           fld.Separator := vartostr(n.Attributes['separator']);
      end;
      if fld.EncryptValue then encrypt_val := vartostr(n.Attributes['env']);
      if not fld.AsGrid then
        dbcall.Parser.RegisterVar(fld.FieldName, dt[1], v)
      else if ((fno=fld.FrameNo) and (rowno=rno)) or ((pfno=fld.FrameNo) and (rowno=prno) ) then
        dbcall.Parser.RegisterVar(fld.FieldName, dt[1], v);

      if ((fld.FrameNo <> lastframeno) or (rowno <> LastRowNo)) and (recid>0)  then
      begin
         if fld.frameno=1 then
         begin
            dbcall.Validate.Parser.RegisterVar('recordid', 'n', srecid);
            dbcall.StoreData.SetRecid(recid);
         end;
         dbcall.storedata.RecordIdList.Add(dbcall.struct.axprovider.dbm.gf.Pad(IntToStr(fld.frameno), 3, ' ')
           + dbcall.struct.axprovider.dbm.gf.Pad(IntToStr(rowno), 5, ' ') + dbcall.struct.axprovider.dbm.gf.pad(floattostr(recid),15,' ')+fld.Tablename);
         LastRowNo := rowno;
         LastFrameNo := fld.FrameNo;
      end;
      if dbcall.StoreData.LastSavedRecordId > 0 then
      begin
        if oldid = 0  then oldid := id;
        if ov <> '' then
        begin
          if pos('<br><br>',ov) > 0  then ov := dbm.gf.FindAndReplace(ov,'<br><br>',#$D#$A);
          if pos('<br>',ov) > 0  then ov := dbm.gf.FindAndReplace(ov,'<br>',#$D);
        end else begin
          if not n.HasAttribute('ov') then
            ov := v;
        end;
      end;
      dbcall.StoreData.DirectSubmitValue(fld, v, ov, rowno, id, oldid, recid,encrypt_val);
      if ((fld.AsGrid) and (fno <> fld.FrameNo)) then
      begin
        dbcall.Parser.RegisterVar(f,dt[1],v);
      end;
    end else begin
      if vartostr(n.Attributes['t']) <> '' then
         dt := vartostr(vartostr(n.Attributes['t']))
      else dt := 'c';
      dbcall.Parser.RegisterVar(f,dt[1],v);
    end;
  end;
  dbcall.storedata.ClientNotify:=true;
  dbm.gf.dodebug.msg('Setting values over');
  dbcall.TrimExtraRows;
  dbcall.StoreData.ins_del_rows.Clear;
  dbcall.StoreData.webdata := false;
  dbcall.StoreData.FieldList.Sort(SortFieldListLoad);
  dbcall.StoreData.AfterSort;
  for j := 0 to dbcall.StoreData.structdef.frames.Count-1 do begin
     if dbcall.StoreData.RowCount(pFrm(dbcall.StoreData.structdef.frames[j]).FrameNo) > 0 then
        pFrm(dbcall.StoreData.structdef.frames[j]).HasDataRows := True;
  end;
  dbcall.Validate.FastDataFlag := FastDataFlag;
end;


procedure TASBDataObj.SubmitClientValuesToSD_ForLoadRec;
var j,fno,rno,rowno,LastRowNo,LastFrameNo,pfno,prno:integer;
    f,v,dt,srecid, ov,s,encrypt_val:String;
    n,rnode : ixmlnode;
    id,oldid,recid : extended;
    fld : pFld;
begin
  dbcall.StoreData.webdata := true;
  dbcall.storedata.ClientNotify:=false;
  dbm.gf.dodebug.msg('Submiting & Registering data ');
  RegisterGlobalVars;
  fno:=0;rno:=0;
  s:=vartostr(xmldoc.DocumentElement.Attributes['frameno']);
  if s<>'' then fno := strtoint(s);
  s:=vartostr(xmldoc.DocumentElement.Attributes['activerow']);
  if s<>'' then rno := strtoint(s);
  if vartype(xmldoc.DocumentElement.Attributes['pdc']) <> varnull then
  begin
     if vartostr(xmldoc.DocumentElement.Attributes['pdc']) <> '' then
        pfno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['pdc']));
  end;
  if vartype(xmldoc.DocumentElement.Attributes['prow']) <> varnull then
  begin
     if vartostr(xmldoc.DocumentElement.Attributes['prow']) <> '' then
        prno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['prow']));
  end;
  rnode := xmldoc.DocumentElement.ChildNodes.FindNode('data'); // his node used for savedata service
  if rnode = nil then rnode := xmldoc.DocumentElement.ChildNodes.FindNode('FieldList'); // This node used for dofillgridvalues service
  if rnode = nil then n := xmldoc.DocumentElement.ChildNodes.FindNode('varlist'); // his node used for action service
  if assigned(n) then rnode := n.ChildNodes[0];
  if rnode = nil then rnode := xmldoc.DocumentElement;
  id := 0; oldid := 0; recid := 0;LastRowNo:=-1; LastFrameNo := -1;
  for j:=0 to rnode.childnodes.count-1 do begin
    n := rnode.childnodes[j];
    f:=vartostr(n.NodeName);
    v:=vartostr(n.NodeValue);
    //This section reads the axp_recid field from XML into recid variable. This is used as recordid for all subsequent elements.
    if copy(f,1,9) = 'axp_recid' then  begin
       recid := dbm.gf.strtofloatz(v);
       srecid := v;
       continue;
    end;
    //Convert special characters to actual characters.

    if pos('<br>',v) > 0  then v := dbm.gf.FindAndReplace(v,'<br>',#$D#$A);
    ov:='';oldid:=0; encrypt_val := '';
    fld:=dbcall.struct.GetField(f);
    if assigned(fld) then
    begin
      //submit to storedata.
      id := dbm.gf.strtofloatz(vartostr(n.attributes['id']));
      oldid := dbm.gf.strtofloatz(vartostr(n.attributes['oldid']));
      dt := fld.DataType;
      if vartostr(n.Attributes['rowno']) <> '' then
         rowno := strtoint(vartostr(n.Attributes['rowno']))
      else rowno := 1;
      if rowno = 0 then rowno := 1;
      ov:=vartostr(n.attributes['ov']);
      if vartostr(n.Attributes['multiselect']) = 'true' then
      begin
        fld.cl_multiselect := true;
        if vartostr(n.Attributes['separator']) = '' then
           fld.Separator := ','
        else
           fld.Separator := vartostr(n.Attributes['separator']);
      end;
      if fld.EncryptValue then encrypt_val := vartostr(n.Attributes['env']);
      if not fld.AsGrid then
        dbcall.Parser.RegisterVar(fld.FieldName, dt[1], v)
      else if ((fno=fld.FrameNo) and (rowno=rno)) or ((pfno=fld.FrameNo) and (rowno=prno) ) then
        dbcall.Parser.RegisterVar(fld.FieldName, dt[1], v);

      if ((fld.FrameNo <> lastframeno) or (rowno <> LastRowNo)) and (recid>0)  then
      begin
         if fld.frameno=1 then
         begin
            dbcall.Validate.Parser.RegisterVar('recordid', 'n', srecid);
            dbcall.StoreData.SetRecid(recid);
         end;
         dbcall.storedata.RecordIdList.Add(dbcall.struct.axprovider.dbm.gf.Pad(IntToStr(fld.frameno), 3, ' ')
           + dbcall.struct.axprovider.dbm.gf.Pad(IntToStr(rowno), 5, ' ') + dbcall.struct.axprovider.dbm.gf.pad(floattostr(recid),15,' ')+fld.Tablename);
         LastRowNo := rowno;
         LastFrameNo := fld.FrameNo;
      end;
      if dbcall.StoreData.LastSavedRecordId > 0 then
      begin
        if oldid = 0  then oldid := id;
        if ov <> '' then
        begin
          if pos('<br><br>',ov) > 0  then ov := dbm.gf.FindAndReplace(ov,'<br><br>',#$D#$A);
          if pos('<br>',ov) > 0  then ov := dbm.gf.FindAndReplace(ov,'<br>',#$D);
        end else begin
          if not n.HasAttribute('ov') then
            ov := v;
        end;
      end;
      //dbcall.StoreData.DirectSubmitValue(fld, v, ov, rowno, id, oldid, recid,encrypt_val);
      //Submivalue checks for fldrec if exists it will update
      dbcall.StoreData.SubmitValue(fld.FieldName, rowno, v, ov, id, oldid, recid,encrypt_val);
    end else begin
      if vartostr(n.Attributes['t']) <> '' then
         dt := vartostr(vartostr(n.Attributes['t']))
      else dt := 'c';
      dbcall.Parser.RegisterVar(f,dt[1],v);
    end;
  end;
  dbcall.storedata.ClientNotify:=true;
  dbm.gf.dodebug.msg('Setting values over');
  dbcall.TrimExtraRows;
  dbcall.StoreData.ins_del_rows.Clear;
  dbcall.StoreData.webdata := false;
  dbcall.StoreData.FieldList.Sort(SortFieldListLoad);
  dbcall.StoreData.AfterSort;
  for j := 0 to dbcall.StoreData.structdef.frames.Count-1 do begin
     if dbcall.StoreData.RowCount(pFrm(dbcall.StoreData.structdef.frames[j]).FrameNo) > 0 then
        pFrm(dbcall.StoreData.structdef.frames[j]).HasDataRows := True;
  end;
  dbcall.Validate.FastDataFlag := FastDataFlag;
end;

procedure TASBDataObj.SubmitClientValuesToSD_ForKeyValueSave;
var j,fno,rno,rowno,LastRowNo,LastFrameNo,pfno,prno:integer;
    f,v,dt,srecid, ov,s,encrypt_val:String;
    n,rnode : ixmlnode;
    id,oldid,recid : extended;
    fld : pFld;
    bGetRecId : Boolean;
begin
  dbcall.StoreData.webdata := true;
  dbcall.storedata.ClientNotify:=false;
  dbm.gf.dodebug.msg('SubmitClientValuesToSD_ForKeyValueSave... ');
  RegisterGlobalVars;
  fno:=0;rno:=0;
  s:=vartostr(xmldoc.DocumentElement.Attributes['frameno']);
  if s<>'' then fno := strtoint(s);
  s:=vartostr(xmldoc.DocumentElement.Attributes['activerow']);
  if s<>'' then rno := strtoint(s);
  if vartype(xmldoc.DocumentElement.Attributes['pdc']) <> varnull then
  begin
     if vartostr(xmldoc.DocumentElement.Attributes['pdc']) <> '' then
        pfno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['pdc']));
  end;
  if vartype(xmldoc.DocumentElement.Attributes['prow']) <> varnull then
  begin
     if vartostr(xmldoc.DocumentElement.Attributes['prow']) <> '' then
        prno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['prow']));
  end;
  rnode := xmldoc.DocumentElement.ChildNodes.FindNode('data'); // his node used for savedata service
  if rnode = nil then rnode := xmldoc.DocumentElement.ChildNodes.FindNode('FieldList'); // This node used for dofillgridvalues service
  if rnode = nil then n := xmldoc.DocumentElement.ChildNodes.FindNode('varlist'); // his node used for action service
  if assigned(n) then rnode := n.ChildNodes[0];
  if rnode = nil then rnode := xmldoc.DocumentElement;
  id := 0; oldid := 0; recid := 0;LastRowNo:=-1; LastFrameNo := -1;
  bGetRecId := False;
  for j:=0 to rnode.childnodes.count-1 do begin
    n := rnode.childnodes[j];
    f:=vartostr(n.NodeName);
    v:=vartostr(n.NodeValue);
    //This section reads the axp_recid field from XML into recid variable. This is used as recordid for all subsequent elements.
    if copy(f,1,9) = 'axp_recid' then  begin
       recid := dbm.gf.strtofloatz(v);
       srecid := v;
       if Not dbcall.bForPayBooks then bGetRecId := true;
       continue;
    end;

    //Convert special characters to actual characters.

    if pos('<br>',v) > 0  then v := dbm.gf.FindAndReplace(v,'<br>',#$D#$A);
    ov:='';oldid:=0; encrypt_val := '';
    fld:=dbcall.struct.GetField(f);
    if assigned(fld) then
    begin

      //submit to storedata.
      id := dbm.gf.strtofloatz(vartostr(n.attributes['id']));
      oldid := dbm.gf.strtofloatz(vartostr(n.attributes['oldid']));
      dt := fld.DataType;
      if vartostr(n.Attributes['rowno']) <> '' then
         rowno := strtoint(vartostr(n.Attributes['rowno']))
      else rowno := 1;
      if rowno = 0 then rowno := 1;

      (*
      the below statements introduced to handle keyvalue save where we do not receive actual recid.
      Getrecid for the first time when row starts
      Currently we have handled for NonGrid , the same has to be implemented for grid as well
      *)
      if bGetRecId then
      begin
        recid := dbcall.StoreData.GetRecordId(fld.FrameNo,rowno);
        srecid := FloattoStr(recid);
        bGetRecId := False;
      end;

      ov:=vartostr(n.attributes['ov']);
      if vartostr(n.Attributes['multiselect']) = 'true' then
      begin
        fld.cl_multiselect := true;
        if vartostr(n.Attributes['separator']) = '' then
           fld.Separator := ','
        else
           fld.Separator := vartostr(n.Attributes['separator']);
      end;
      if fld.EncryptValue then encrypt_val := vartostr(n.Attributes['env']);
      if not fld.AsGrid then
        dbcall.Parser.RegisterVar(fld.FieldName, dt[1], v)
      else if ((fno=fld.FrameNo) and (rowno=rno)) or ((pfno=fld.FrameNo) and (rowno=prno) ) then
        dbcall.Parser.RegisterVar(fld.FieldName, dt[1], v);

      if ((fld.FrameNo <> lastframeno) or (rowno <> LastRowNo)) and (recid>0)  then
      begin
         if fld.frameno=1 then
         begin
            dbcall.Validate.Parser.RegisterVar('recordid', 'n', srecid);
            dbcall.StoreData.SetRecid(recid);
         end;
         dbcall.storedata.RecordIdList.Add(dbcall.struct.axprovider.dbm.gf.Pad(IntToStr(fld.frameno), 3, ' ')
           + dbcall.struct.axprovider.dbm.gf.Pad(IntToStr(rowno), 5, ' ') + dbcall.struct.axprovider.dbm.gf.pad(floattostr(recid),15,' ')+fld.Tablename);
         LastRowNo := rowno;
         LastFrameNo := fld.FrameNo;
      end;
      if dbcall.StoreData.LastSavedRecordId > 0 then
      begin
        if oldid = 0  then oldid := id;
        if ov <> '' then
        begin
          if pos('<br><br>',ov) > 0  then ov := dbm.gf.FindAndReplace(ov,'<br><br>',#$D#$A);
          if pos('<br>',ov) > 0  then ov := dbm.gf.FindAndReplace(ov,'<br>',#$D);
        end else begin
          if not n.HasAttribute('ov') then
            ov := v;
        end;
      end;
      //dbcall.StoreData.DirectSubmitValue(fld, v, ov, rowno, id, oldid, recid);
      //Submivalue checks for fldrec if exists it will update
      dbcall.StoreData.SubmitValue(fld.FieldName, rowno, v, ov, id, oldid, recid,encrypt_val);
    end else begin
      if vartostr(n.Attributes['t']) <> '' then
         dt := vartostr(vartostr(n.Attributes['t']))
      else dt := 'c';
      dbcall.Parser.RegisterVar(f,dt[1],v);
    end;
  end;

  dbcall.storedata.ClientNotify:=true;
  dbm.gf.dodebug.msg('Setting values over');
  dbcall.TrimExtraRows;
  dbcall.StoreData.ins_del_rows.Clear;
  dbcall.StoreData.webdata := false;
  dbcall.StoreData.FieldList.Sort(SortFieldListLoad);
  dbcall.StoreData.AfterSort;
  for j := 0 to dbcall.StoreData.structdef.frames.Count-1 do begin
     if dbcall.StoreData.RowCount(pFrm(dbcall.StoreData.structdef.frames[j]).FrameNo) > 0 then
        pFrm(dbcall.StoreData.structdef.frames[j]).HasDataRows := True;
  end;
  dbcall.Validate.FastDataFlag := FastDataFlag;
end;


Procedure TASBDataObj.RegisterGlobalVars;
var n,rnode : ixmlnode;
    j : integer;
    f,v,dt : String;
begin
  dbm.gf.dodebug.msg('Registering Global variables data ');
  rnode := xmldoc.DocumentElement.ChildNodes.FindNode('globalvars');
  if rnode <> nil then
  begin
    for j:=0 to rnode.childnodes.count-1 do begin
      n := rnode.childnodes[j];
      f:=vartostr(n.NodeName);
      v:=vartostr(n.NodeValue);
      if vartype(n.Attributes['dt']) <> varnull then
         dt := vartostr(n.Attributes['dt'])
      else dt := 'c';
      dbcall.Parser.RegisterVar(f,dt[1],v);
      dbm.gf.dodebug.msg('Register Global Var : '+ f + '=' +v);
    end;
  end;
  dbm.gf.dodebug.msg('Registering Global variables data over');
end;
{
Procedure TASBDataObj.RegisterConfigData(s : string);
var j : integer;
    f,v,dt : String;
    jsnObject,JsonObj1: TJsonObject;
    jsnPair : TJSONPair;
begin
  dbm.gf.dodebug.msg('Registering ConfigData ');
  jsnObject := nil;
  jsnObject := TJSONObject.ParseJSONValueUTF8(TEncoding.UTF8.GetBytes(s), 0) as TJsonObject;
  if Assigned(jsnObject) then
  begin
   JsonObj1 := jsnObject.Get(0).JsonValue as TJSONObject;
   if Assigned(JsonObj1) then
   begin
     try
       for jsnPair in JsonObj1 do
       begin
           f := jsnPair.JsonString.Value;// ToString;
           v := jsnPair.JsonValue.Value;// ToString;
//           v := MakeValidXMLString(v);
           if v <> '' then
           begin
             if pos('<br><br>',v) > 0  then v := dbm.gf.FindAndReplace(v,'<br><br>',#$D#$A);
             if pos('<br>',v) > 0  then v := dbm.gf.FindAndReplace(v,'<br>',#$D);
//             if pos('^^dq',v) > 0  then  v := dbm.gf.FindAndReplace(v, '^^dq', '"');
           end;

           dbm.gf.DoDebug.msg(Format('1) Key=%s Value=%s',[f,v]));
           dbcall.Parser.RegisterVar(f,'c',v);
       end;
     finally
       jsnObject.Free;
     end;
   end;
  end;
  dbm.gf.dodebug.msg('Registering ConfigData over');
end;

Procedure TASBDataObj.CreateInitialSD(struct : TStructDef;  transid : String);
  var i : integer;
    tRec : pFieldRec;
begin
  orgSD := TStoreData.Create(struct, transid);
  for i := 0 to dbcall.StoreData.FieldList.Count - 1 do begin
    trec := pFieldRec(dbcall.StoreData.FieldList[i]);
    orgSD.SubmitValue(trec.FieldName,trec.RowNo,trec.Value,'',trec.IdValue,0,0);
  end;
end;
}
function TASBDataObj.CheckForAccess(sname : String) : String;
  var accxml : ixmldocument;
begin
  result := '';
  if pos('default',dbm.gf.userroles) = 0 then
  begin
    ASBCommonObj.MakeInUserRoles;
    accxml := axprovider.CheckStructureAccess('tstructs',sname);
    if accxml = nil then raise Exception.create('User does not have access to this structure...');
    result := GetTransControlString('t','',accxml);
    TransAccess := result;
  end;
end;

function TASBDataObj.GetTransControlString(cstr,tname:String;accxml : ixmldocument):String;
var enode,tnode,n,x : ixmlnode;
    ExprStr,v1,v2,val : String;
    i,j : integer;
    ExprList,CondList,CondFldList:TStringList;
begin
  if not assigned(accxml) then exit;
  dbm.gf.dodebug.msg('Executing GetTransControlString');
  enode := accxml.DocumentElement;
  if (enode = nil) then exit;
  ExprStr := '';
  ExprList := TStringList.Create;
  CondList := TStringList.Create;
  CondFldList := TStringList.Create;
  for i := 0 to enode.ChildNodes.Count - 1 do begin
    tnode := enode.ChildNodes[i].ChildNodes[0];
    x := tnode.ChildNodes.FindNode('tc');   //transcontrol
    if assigned(x) then begin
      for j := 0 to x.ChildNodes.Count - 1 do begin
        n := x.ChildNodes[j];
        val := vartostr(n.NodeValue);
        if (n.Attributes['op'] = 'between') then begin
          v1 := dbm.gf.getnthstring(val,1);
          v2 := dbm.gf.getnthstring(val,2);
        end else begin
          v1 := val;
          v2 := '';
        end;
        ExprList.Add(ConvertToExpr(vartostr(n.Attributes['f']),vartostr(n.Attributes['d']),vartostr(n.Attributes['op']),v1,v2));
        ExprStr := ExprStr + vartostr(n.Attributes['a']);
        if (vartostr(n.Attributes['a']) = 'n') then begin
          CondList.Add(ConvertToCondStr(vartostr(n.Attributes['f']),vartostr(n.Attributes['d']),vartostr(n.Attributes['op']),v1,v2));
          CondFldList.Add(vartostr(n.Attributes['f'])+','+vartostr(n.Attributes['d'])+','+v1+','+v2);
        end;
      end;
    end;
  end;
  if (cstr = 't') then
    result := GetTransControl(exprlist,exprstr)
  else
    result := FrameCondStr(condlist,condfldlist,tname);
  ExprList.Free;CondList.Free;CondFldList.Free;
end;


function TASBDataObj.ConvertToExpr(fldname,dtype,op,fldvalue,fldvalue1: String) : String ;
begin
  dbm.gf.dodebug.msg('Executing ConvertToExprg');
   result := '';
   if (op = 'le') then begin
     if copy(fldValue,1,1) = ':' then begin
       delete(fldValue,1,1);
       result := 'iif ((' + fldName + ' < ' + fldValue + ') | (' + fldName + ' = ' + fldValue + '), {T} , {F})' ;
     end else begin
       if (dtype = 'd') then
         result := 'iif ((' + fldName + ' < ctod({' + fldValue + '})) | (' + fldName + ' = ctod({' + fldValue + '})), {T} , {F})'
       else
         result := 'iif ((' + fldName + ' < {' + fldValue + '}) | (' + fldName + ' = {' + fldValue + '}), {T} , {F})' ;
     end;
   end else if (op = 'ge') then begin
     if copy(fldValue,1,1) = ':' then begin
       delete(fldValue,1,1);
       result := 'iif ((' + fldName + ' > ' + fldValue + ') | (' + fldName + ' = ' + fldValue + '), {T} , {F})' ;
     end else begin
       if (dtype = 'd') then
         result := 'iif ((' + fldName + ' > ctod({' + fldValue + '})) | (' + fldName + ' = ctod({' + fldValue + '})), {T} , {F})'
       else
         result := 'iif ((' + fldName + ' > {' + fldValue + '}) | (' + fldName + ' = {' + fldValue + '}), {T} , {F})' ;
     end;
   end
   else if (op = 'ne') then begin
     if copy(fldValue,1,1) = ':' then begin
       delete(fldValue,1,1);
       result := 'iif (' + fldName + ' # ' + fldValue + ' , {T} , {F})' ;
     end else begin
       if (dtype = 'd') then
         result := 'iif (' + fldName + ' # ctod({' + fldValue + '}) , {T} , {F})'
       else
         result := 'iif (' + fldName + ' # {' + fldValue + '} , {T} , {F})' ;
     end;
   end else if (op = 'eq') then begin
     if copy(fldValue,1,1) = ':' then begin
       delete(fldValue,1,1);
       result := 'iif (' + fldName + ' = ' + fldValue + ' , {T} , {F})' ;
     end else begin
       if (dtype = 'd') then
         result := 'iif (' + fldName + ' = ctod({' + fldValue + '}) , {T} , {F})'
       else
         result := 'iif (' + fldName + ' = {' + fldValue + '} , {T} , {F})' ;
     end;
   end else if (op = 'lt') then begin
     if copy(fldValue,1,1) = ':' then begin
       delete(fldValue,1,1);
         result := 'iif (' + fldName + ' < ' + fldValue + ' , {T} , {F})' ;
     end else begin
       if (dtype = 'd') then
         result := 'iif (' + fldName + ' < ctod({' + fldValue + '}) , {T} , {F})'
       else
         result := 'iif (' + fldName + ' < {' + fldValue + '} , {T} , {F})' ;
     end;
   end else if (op = 'gt') then begin
     if copy(fldValue,1,1) = ':' then begin
       delete(fldValue,1,1);
       result := 'iif (' + fldName + ' > ' + fldValue + ' , {T} , {F})' ;
     end else begin
       if (dtype = 'd') then
         result := 'iif (' + fldName + ' > ctod({' + fldValue + '}) , {T} , {F})'
       else
         result := 'iif (' + fldName + ' > ' + fldValue + ' , {T} , {F})' ;
     end;
   end else if (op = 'btw') then begin
     if (copy(fldValue,1,1) = ':') and (copy(fldValue1,1,1) = ':') then begin
       delete(fldValue,1,1);
       delete(fldValue1,1,1);
       result := 'iif ((' + fldName + ' > ' + fldValue + ') | (' + fldName + ' = ' + fldValue + ') & (' + fldName + ' > ' + fldValue1 + ') | (' + fldName + ' = ' + fldValue1 + '), {T} , {F})' ;
     end else if (copy(fldValue,1,1) = ':') and (copy(fldValue1,1,1) <> ':') then begin
       delete(fldValue,1,1);
       if (dtype = 'd') then
         result := 'iif ((' + fldName + ' > ' + fldValue + ') | (' + fldName + ' = ' + fldValue + ') & (' + fldName + ' > ctod({' + fldValue1 + '})) | (' + fldName + ' = ctod({' + fldValue1 + '})), {T} , {F})'
       else
         result := 'iif ((' + fldName + ' > ' + fldValue + ') | (' + fldName + ' = ' + fldValue + ') & (' + fldName + ' > {' + fldValue1 + '}) | (' + fldName + ' = {' + fldValue1 + '}), {T} , {F})' ;
     end else if (copy(fldValue,1,1) <> ':') and (copy(fldValue1,1,1) = ':') then begin
       delete(fldValue,1,1);
       if (dtype = 'd') then
         result := 'iif ((' + fldName + ' > ctod({' + fldValue + '})) | (' + fldName + ' = {' + fldValue + '}) & (' + fldName + ' > ' + fldValue1 + ') | (' + fldName + ' = ' + fldValue1 + '), {T} , {F})'
       else
         result := 'iif ((' + fldName + ' > {' + fldValue + '}) | (' + fldName + ' = {' + fldValue + '}) & (' + fldName + ' > ' + fldValue1 + ') | (' + fldName + ' = ' + fldValue1 + '), {T} , {F})' ;
     end else begin
       if (dtype = 'd') then
         result := 'iif ((' + fldName + ' > ctod({' + fldValue + '})) | (' + fldName + ' = ctod({' + fldValue + '})) & (' + fldName + ' > ctod({' + fldValue1 + '})) | (' + fldName + ' = ctod({' + fldValue1 + '})), {T} , {F})'
       else
         result := 'iif ((' + fldName + ' > {' + fldValue + '}) | (' + fldName + ' = {' + fldValue + '}) & (' + fldName + ' > {' + fldValue1 + '}) | (' + fldName + ' = {' + fldValue1 + '}), {T} , {F})' ;
     end;
   end else if (op = 'isempty') then   begin
     result := 'isemptyvalue (' + fldName + ' , {' + dType + '})' ;
   end ;
end;


function TASBDataObj.ConvertToCondStr(fldname,dtype,op,fldvalue,fldvalue1: String) : String ;
var fv,s : String;
    i : integer;
begin
   dbm.gf.dodebug.msg('Executing ConvertToCondStr');
   result := '';
   fldname := '$$'+trim(fldname)+'$$';
   if (op <> 'in') and (op <> 'notin') then begin
     if (dtype = 'd') then
       fv := 'to_date('+quotedstr(fldValue)+',''dd/mm/yyyy'')'
     else if (dtype = 'n') then
       fv := fldValue
     else
       fv := quotedstr(fldValue);
   end else begin
     i := 1;
     s := dbm.gf.getnthstring(fldvalue,i);
     fv := '';
     while (s<>'') do begin
       if (dtype = 'd') then
         fv := fv+','+'to_date('+quotedstr(s)+',''dd/mm/yyyy'')'
       else if (dtype = 'n') then
         fv := fv+','+s
       else
         fv := fv+','+quotedstr(s);
       inc(i);
       s := dbm.gf.getnthstring(fldvalue,i);
     end;
     delete(fv,1,1);
   end;
   if (op = 'le') then begin
     if (dtype = 'd') then
       result := 'not('+fldName + ' <= '+fv+')'
     else if (dtype = 'n') then
       result := 'not('+fldName + ' <= ' + fv+')'
     else
       result := 'not('+fldName + ' <= ' + fv+')';
   end else if (op = 'ge') then begin
     if (dtype = 'd') then
       result := 'not('+fldName + ' >= '+fv+')'
     else if (dtype = 'n') then
       result := 'not('+fldName + ' >= ' + fv+')'
     else
       result := 'not('+fldName + ' >= ' + fv+')';
   end
   else if (op = 'ne') then begin
     if (dtype = 'd') then
       result := 'not('+fldName + ' != '+fv+')'
     else if (dtype = 'n') then
       result := 'not('+fldName + ' != ' + fv+')'
     else
       result := 'not('+fldName + ' != ' + fv+')';
   end else if (op = 'eq') then begin
     if (dtype = 'd') then
       result := 'not('+fldName + ' = '+fv+')'
     else if (dtype = 'n') then
       result := 'not('+fldName + ' = ' +fv+')'
     else
       result := 'not('+fldName + ' = ' + fv+')';
   end else if (op = 'lt') then begin
     if (dtype = 'd') then
       result := 'not('+fldName + ' < '+fv+')'
     else if (dtype = 'n') then
       result := 'not('+fldName + ' < ' + fv+')'
     else
       result := 'not('+fldName + ' < ' + fv+')';
   end else if (op = 'gt') then begin
     if (dtype = 'd') then
       result := 'not('+fldName + ' > '+fv+')'
     else if (dtype = 'n') then
       result := 'not('+fldName + ' > ' + fv+')'
     else
       result := 'not('+fldName + ' > ' + fv+')';
   end else if (op = 'btw') then begin
     if (dtype = 'd') then
       result := 'not('+fldName + ' between to_date(' +quotedstr(fldValue)+ ',''dd/mm/yyyy'') and to_date('+ quotedstr(fldvalue1)+',''''dd/mm/yyyy''''))'
     else if (dtype = 'c') then
       result := 'not('+fldName + ' between ' + quotedstr(fldValue)+ ' and '+ quotedstr(fldvalue1)+')'
     else
       result := 'not('+fldName + ' between ' + fldValue+ ' and '+ fldvalue1+')';
   end else if (op = 'isempty') then  begin
     result := 'not('+fldname+' is null)' ;
   end else if (op = 'in') then  begin
     result := 'not('+fldname+' in ('+fv+'))' ;
   end else if (op = 'notin') then  begin
     result := 'not('+fldname+' not in ('+fv+'))' ;
   end ;
end;

function TASBDataObj.GetTransControl(exprlist:TStringList;exprstr:String):String;
var i,tcno,val : integer;
    tc : String;
begin
  dbm.gf.dodebug.msg('Executing GetTransControl');
  result := '';
  tc := 'nved';
  tcno := 4;
  for i := 0 to exprList.Count - 1 do begin
    dbcall.parser.Evaluate(exprList[i]);
    if (dbcall.parser.Value = 'T') then begin
      val := pos(exprStr[i+1],tc);
      if (val < tcno) then
        tcno := val;
      if (tcno = 1) then begin
        result := 'n';
        exit;
      end;
    end;
  end;
  result := tc[tcno];
end;

function TASBDataObj.FrameCondStr(condlist,condfldlist:TStringList;tname:String):String;
var i : integer;
   s1,s2,s,val : String;
   fld : pFld;
begin
  result := '';
  dbm.gf.dodebug.msg('Executing FrameCondStr function');
  for i := 0 to CondList.Count - 1 do begin
    s1 := CondList[i];
    s2 := CondFldList[i];
    s := dbm.gf.getnthstring(s2,1);
    dbcall := TDbCall.create;
    dbcall.axprovider := Axprovider;
    dbcall.dbm := dbm;
    dbcall.transid := tname;
    fld := dbcall.struct.GetField(s);
    if fld.SourceKey then
     val := fld.Sourcetable+'.'+fld.FieldName
    else
     val := fld.Tablename+'.'+fld.FieldName;
    s := '$$'+fld.FieldName+'$$';
    s1 := dbm.gf.FindAndReplace(s1,s,val);
    s := dbm.gf.getnthstring(s2,3);
    if (copy(s,1,1) = ':') then begin
      val := copy(s,2,100);
      fld := dbcall.struct.GetField(val);
      if (fld <> nil) then begin
        if fld.SourceKey then
         val := fld.Sourcetable+'.'+fld.FieldName
        else
         val := fld.Tablename+'.'+fld.FieldName;
      end else
         val := dbcall.Parser.GetVarValue(val);
      s1 := dbm.gf.FindAndReplace(s1,s,val);
    end;
    s := dbm.gf.getnthstring(s2,4);
    if (copy(s,1,1) = ':') then begin
      val := copy(s,2,100);
      fld := dbcall.struct.GetField(val);
      if (fld <> nil) then
         val := fld.Tablename+'.'+fld.FieldName
      else
         val := dbcall.parser.GetVarValue(val);
      s1 := dbm.gf.FindAndReplace(s1,s,val);
    end;
    result := result+' and '+s1;
  end;
  delete(result,1,5);
end;

procedure  TASBDataObj.CreateMessageNode;
begin
  if act.msgnode = '{"message":[{"msg":"' then act.msgnode := ''
  else begin
    delete(act.msgnode,length(act.msgnode),1);
    act.msgnode := act.msgnode + '"}]}';
    if (not dbm.gf.MobileWSFlag) and (jsonstr <> '') then jsonstr := jsonstr +  '*$*' + act.msgnode
    else if (dbm.gf.MobileWSFlag) and (jsonstr <> '') then jsonstr := jsonstr +  ',' + copy(act.msgnode,2,length(trim(act.msgnode))-2)
    else jsonstr := jsonstr + act.msgnode ;
  end;
end;

procedure  TASBDataObj.CreateFormControlNode;
begin
  if (TransAccess = 'v') then
     act.formControlNode := act.formControlNode + '{"readonlytrans":"yes"},' ;
  if act.formControlNode = '{"formcontrol":[' then act.formControlNode := ''
  else begin
    delete(act.formControlNode,length(act.formControlNode),1);
    act.formControlNode := act.formControlNode + ']}';
    if (not dbm.gf.MobileWSFlag) and (jsonstr <> '') then jsonstr := jsonstr + '*$*' +  act.formControlNode
    else if (dbm.gf.MobileWSFlag) and (jsonstr <> '') then jsonstr := jsonstr + ',' +  copy(act.formControlNode,2,length(trim(act.formControlNode))-2)
    else jsonstr := jsonstr + act.formControlNode ;
  end;
end;

procedure  TASBDataObj.CreateCommandNode;
  var s : string;
begin
  if assigned(dbcall) then
  begin
    if dbcall.StoreData.Cancelled then
    begin
      act.createcmdnode('canceled','yes')  ;
      act.cmdnode := act.cmdnode + ',"reason":"'+dbcall.storedata.cancelRemarks+ ' by ' + dbcall.storedata.SUserName + ' on ' + dbcall.storedata.SModifiedOn + '"';
      act.cmdnode := act.cmdnode + '},';
    end
    else if dbcall.RecordLocked then begin
      act.createcmdnode('recordlocked','true')  ;
      act.cmdnode := act.cmdnode +',"lockedby":"'+ dbm.gf.RecordLockedBY+'"';
      act.cmdnode := act.cmdnode + '},';
    end;
  end;
  if SaveNodeFromScripts <> '' then
  begin
     if act.cmdnode = '' then act.cmdnode := SaveNodeFromScripts
     else begin
       s := lowercase(act.cmdnode);
       if (pos('"opentstruct"', s) = 0) and (pos('"loadtstruct"', s) = 0) and
       (pos('"openpage"', s) = 0) and (pos('"loadpage"', s) = 0) and
       (pos('"openiview"', s) = 0) and (pos('"loadiview"', s) = 0) then
       begin
         SaveNodeFromScripts := SaveNodeFromScripts + act.cmdnode;
         act.cmdnode := SaveNodeFromScripts;
         SaveNodeFromScripts := '';
       end else act.cmdnode := '{"command":[' + act.cmdnode;
     end;
  end;
  if act.cmdnode = '{"command":[' then act.cmdnode := ''
  else begin
    delete(act.cmdnode,length(act.cmdnode),1);
    act.cmdnode := act.cmdnode + ']}';
    if jsonstr <> '' then jsonstr := jsonstr + '*$*' +  act.cmdnode
    else jsonstr := jsonstr + act.cmdnode ;
  end;
end;

procedure  TASBDataObj.CreateAttachmentNode;
   var attachnode : ixmlnode;
   fvalue,ttable : String;
  SR      : TSearchRec;
  DirList : TStrings;
  sfname, tfname : string;
begin
   if not assigned(dbcall) then exit;
   atnode := '';
   if AxmAttachMode = 'fs' then
   begin
    if dbm.gf.SessionId = '' then
      tfname := GetCurrentDir + '\MobileLog\'+dbm.gf.username+'\'
    else
      tfname := GetCurrentDir + '\MobileLog\'+dbm.gf.SessionId+'\';
    fvalue := dbcall.Parser.GetVarValue('recordid');
    sfname := AxmAttachDir + '\' + dbcall.struct.Transid + '\' + fvalue + '\';
    if not DirectoryExists(sfname) then Exit;
    DirList:=TStringList.Create;
    try
      if FindFirst(sfname + '*.*', faArchive, SR) = 0 then
      begin
        repeat
          DirList.Add(SR.Name); //Fill the list
          FileCopy(sfname + SR.Name, tfname + SR.Name);
        until FindNext(SR) <> 0;
        sysutils.FindClose(SR);
      end;
      fvalue := DirList.CommaText;
      if fvalue <> '' then
        atnode := '"attachment":[{"att":"' + fvalue + '"}]';
    finally
      DirList.Free;
    end;
   end
   else
   begin
   if dbCall.struct.XML <> nil then
    attachnode := dbCall.struct.XML.DocumentElement.ChildNodes[0];
   if assigned(attachnode) then
   begin
     if attachnode.ChildValues['a18'] = 'True' then
     begin
        if dbcall.struct.SchemaName = '' then ttable := dbcall.struct.Transid
        else ttable :=dbcall.struct.SchemaName + '.' + dbcall.struct.Transid  ;
        fvalue := Axprovider.GetAttachmentAsStr(ttable,strtofloat(dbcall.Parser.GetVarValue('recordid')));
        if fvalue <> '' then
        begin
          atnode := '{"attachment":[{"att":"';
          atnode := atnode + fvalue + '"}]}';
        end;
     end;
   end;
   end;
   if (not dbm.gf.MobileWSFlag) and (jsonstr <> '') and (atnode <> '')  then jsonstr := jsonstr +  '*$*' + atnode
   else if (dbm.gf.MobileWSFlag) and (jsonstr <> '') and (atnode <> '') then jsonstr := jsonstr + ',' + atnode;

end;

procedure  TASBDataObj.CreateWorkflowNode;
var
  sqlstring, wfstatus, wflevel,un, uc :string;
  maxLevel : Integer;
  x:txds;
  enode : IXMLNode;
begin
  if not assigned(dbcall) then exit;
  wfnode := '';
  if assigned(dbcall.ApprovalNode) then
  begin
     wfnode := wfnode + '{"wfnode":[';
     wfnode := wfnode + '{"btn":"'+vartostr(dbcall.ApprovalNode.Attributes['btn'])+'"}';
     wfnode := wfnode + ',{"status":"'+vartostr(dbcall.ApprovalNode.Attributes['status'])+'"}';
     wfnode := wfnode + ',{"appstatus":"'+vartostr(dbcall.ApprovalNode.Attributes['appstatus'])+'"}';
     wfnode := wfnode + ',{"lno":"'+vartostr(dbcall.ApprovalNode.Attributes['lno'])+'"}';
     wfnode := wfnode + ',{"elno":"'+vartostr(dbcall.ApprovalNode.Attributes['elno'])+'"}';
     wfnode := wfnode + ',{"allowedit":"'+vartostr(dbcall.ApprovalNode.Attributes['allowedit'])+'"}';
     wfnode := wfnode + ',{"allowcancel":"'+vartostr(dbcall.ApprovalNode.Attributes['allowcancel'])+'"}';
     wfnode := wfnode + ',{"readonlyform":"'+vartostr(dbcall.ApprovalNode.Attributes['readonlyform'])+'"}';
     wfnode := wfnode + ',{"dlgusr":"'+vartostr(dbcall.ApprovalNode.Attributes['dlgusr'])+'"}';
     //wfnode := wfnode + ']}';

    enode := dbcall.WorkFlow.wfXML.DocumentElement;
    maxLevel := strtoint(vartostr(enode.Attributes['maxlevel']));
    wfnode := wfnode + ',{"maxlevel":"'+IntToStr(maxLevel)+'"}';
    wflevel := '';
    x := axprovider.dbm.GetXDS(nil);
    x.buffered := True;
//    sqlstring := 'select a.updusername, a.app_datetime, a.app_level, a.app_desc from '+dbcall.WorkFlow.Transid+'workflow a join '+
//                    '(select recordid, app_level, updusername, max(TO_DATE(app_datetime,''dd-mm-yyyy hh24:mi:ss'')) as app_datetime from '+dbcall.WorkFlow.Transid+'workflow where '+
//                    'recordid = ' + QuotedStr(dbcall.WorkFlow.Wrecordid) + ' and app_desc>0 group by recordid, app_level, updusername ) b on a.recordid=b.recordid '+
//                    'and a.app_level=b.app_level and a.updusername=b.updusername and a.app_datetime=b.app_datetime and not exists (select towhom from axtasks c where c.sname='+
//                    QuotedStr(dbcall.WorkFlow.Transid) + ' and c.recordid = b.recordid and c.status=1 and c.towhom=a.updusername and (c.app_level+1)=a.app_level)'+
//                    'union all '+
//                    'select towhom, upddatetime, app_level+1, -1 as status from axtasks where sname='+QuotedStr(dbcall.WorkFlow.Transid)+' and '+
//                    'recordid = ' + QuotedStr(dbcall.WorkFlow.Wrecordid) + ' and status=1 order by 2,3 ';

    if dbm.Connection.dbType='oracle' then
      sqlstring := 'select a.comments,a.updusername, TO_DATE(a.app_datetime,''dd-mm-yyyy hh24:mi:ss'') as app_datetime, a.app_level, a.app_desc from '+dbcall.WorkFlow.Transid+'workflow  a join '+
                     '(select recordid, app_level, updusername, max(TO_DATE(app_datetime,''dd-mm-yyyy hh24:mi:ss'')) as app_datetime from '+dbcall.WorkFlow.Transid+'workflow where '+
                     'recordid = '+dbcall.WorkFlow.Wrecordid + ' and app_desc>0 group by recordid, app_level, updusername ) b on a.recordid=b.recordid '+
                     'and a.app_level=b.app_level and a.updusername=b.updusername and TO_DATE(a.app_datetime,''dd-mm-yyyy hh24:mi:ss'')=b.app_datetime and not exists (select towhom from axtasks c where c.sname='+
                     QuotedStr(dbcall.WorkFlow.Transid) + ' and c.recordid = b.recordid and c.status=1 and c.towhom=a.updusername and (c.app_level+1)=a.app_level) '+
                     'union all '+
                     'select '''' as comments,towhom, TO_DATE(upddatetime,''dd-mm-yyyy hh24:mi:ss'') as upddatetime, app_level+1, -1 as status from axtasks where sname='+QuotedStr(dbcall.WorkFlow.Transid)+' and '+
                     'recordid = '+dbcall.WorkFlow.Wrecordid+' and status=1 order by 4 '   //order by 3
    else if dbm.Connection.dbType='mysql' then
      sqlstring := 'select a.comments,a.updusername,  convert(a.app_datetime, datetime) as app_datetime, a.app_level, a.app_desc from '+dbcall.WorkFlow.Transid+'workflow  a join '+
                     '(select recordid, app_level, updusername, max(convert(app_datetime, datetime)) as app_datetime from '+dbcall.WorkFlow.Transid+'workflow where '+
                     'recordid = '+dbcall.WorkFlow.Wrecordid + ' and app_desc>0 group by recordid, app_level, updusername ) b on a.recordid=b.recordid '+
                     'and a.app_level=b.app_level and a.updusername=b.updusername and convert(a.app_datetime, datetime)=b.app_datetime and not exists (select towhom from axtasks c where c.sname='+
                     QuotedStr(dbcall.WorkFlow.Transid) + ' and c.recordid = b.recordid and c.status=1 and c.towhom=a.updusername and (c.app_level+1)=a.app_level) '+
                     'union all '+
                     'select '''' as comments,towhom, convert(upddatetime,datetime) as upddatetime, app_level+1, -1 as status from axtasks where sname='+QuotedStr(dbcall.WorkFlow.Transid)+' and '+
                     'recordid = '+dbcall.WorkFlow.Wrecordid+' and status=1 order by 4 '
    else if dbm.Connection.dbType='ms sql' then
      sqlstring := 'select a.comments,a.updusername, CONVERT(datetime, a.app_datetime, 101) as app_datetime, a.app_level, a.app_desc from '+dbcall.WorkFlow.Transid+'workflow  a join '+
                     '(select recordid, app_level, updusername, max(CONVERT(datetime, app_datetime, 101)) as app_datetime from '+dbcall.WorkFlow.Transid+'workflow where '+
                     'recordid = '+dbcall.WorkFlow.Wrecordid + ' and app_desc>0 group by recordid, app_level, updusername ) b on a.recordid=b.recordid '+
                     'and a.app_level=b.app_level and a.updusername=b.updusername and CONVERT(datetime, a.app_datetime, 101)=b.app_datetime and not exists (select towhom from axtasks c where c.sname='+
                     QuotedStr(dbcall.WorkFlow.Transid) + ' and c.recordid = b.recordid and c.status=1 and c.towhom=a.updusername and (c.app_level+1)=a.app_level) '+
                     'union all '+
                     'select '''' as comments,towhom,CONVERT(datetime,upddatetime, 101) as upddatetime, app_level+1, -1 as status from axtasks where sname='+QuotedStr(dbcall.WorkFlow.Transid)+' and '+
                     'recordid = '+dbcall.WorkFlow.Wrecordid+' and status=1 order by 4 '
    else if dbm.Connection.dbType='postgre' then
      sqlstring := 'select a.comments, a.updusername, TO_TimeStamp(a.app_datetime,''dd-mm-yyyy hh24:mi:ss'')::timestamp as app_datetime, a.app_level, a.app_desc from '+dbcall.WorkFlow.Transid+'workflow  a join '+
                     '(select recordid, app_level, updusername, max(TO_TimeStamp(app_datetime,''dd-mm-yyyy hh24:mi:ss''))::timestamp as app_datetime from '+dbcall.WorkFlow.Transid+'workflow where '+
                     'recordid = '+dbcall.WorkFlow.Wrecordid + ' and app_desc>0 group by recordid, app_level, updusername ) b on a.recordid=b.recordid '+
                     'and a.app_level=b.app_level and a.updusername=b.updusername and TO_TimeStamp(a.app_datetime,''dd-mm-yyyy hh24:mi:ss'')::timestamp =b.app_datetime and not exists (select towhom from axtasks c where c.sname='+
                     QuotedStr(dbcall.WorkFlow.Transid) + ' and c.recordid = b.recordid and c.status=1 and c.towhom=a.updusername and (c.app_level+1)=a.app_level) '+
                     'union all '+
                     'select '''' as comments,towhom, TO_TimeStamp(upddatetime,''dd-mm-yyyy hh24:mi:ss'')::timestamp as upddatetime, app_level+1, -1 as status from axtasks where sname='+QuotedStr(dbcall.WorkFlow.Transid)+' and '+
                     'recordid = '+dbcall.WorkFlow.Wrecordid+' and status=1 order by 4 ' ;

    x.CDS.CommandText := sqlstring;
    dbm.gf.doDebug.Msg('SQL : ' + sqlstring);
    x.open;
    while not x.cds.Eof do
    begin
      if x.cds.FieldByName('app_desc').AsInteger = -1 then
        wfstatus := 'Pending'
      else
        wfstatus := AxProvider.dbm.gf.GetWFString(x.cds.FieldByName('app_desc').AsInteger);
      if wfstatus ='Approve' then wfstatus:='Approved'
      else if wfstatus='Review' then wfstatus:='Forwarded'
      else if wfstatus='Return' then wfstatus:='Returned';

      if lowercase(copy(x.CDS.FieldByName('comments').AsString,1,13)) = 'autoforwarded' then begin
        wfstatus:= 'Autoforwarded';
        un := 'system';
      end else if lowercase(copy(x.CDS.FieldByName('comments').AsString,1,12)) = 'autoapproved' then begin
        wfstatus:= 'Autoapproved';
        un := 'system';
      end else if lowercase(copy(x.CDS.FieldByName('comments').AsString,1,12)) = 'autoreturned' then begin
        wfstatus:= 'Autoreturned';
        un := 'system';
      end else
      begin
        un := x.cds.FieldByName('updusername').AsString;
        if dbcall.workflow.NickNameList.Count > 0 then
          uc := dbcall.WorkFlow.NickNameList.Values[un]
        else
          uc := '';
      end;

      wflevel := wflevel + ',' + '{"lno" : "'+ x.cds.FieldByName('app_level').AsString+'", "un" : "'+un+
                            '","uc" : "'+uc+'", "st" : "'+ wfstatus +
                            '", "dt" : "'+x.cds.FieldByName('app_datetime').AsString+'"}';
                            //, "nou" : "1"
      x.cds.Next;
    end;
    if assigned(x) then
    begin
      x.close;
      x.Free;
      x := nil;
    end;
    delete(wflevel, 1, 1);
//    {"levels":[ <wflevel> ]}
    wflevel := ', {"levels":[' + wflevel + ']}';
    wfnode := wfnode + wflevel + ']}';
  end;
  if (not dbm.gf.MobileWSFlag) and (jsonstr <> '') and (wfnode <> '')  then jsonstr := jsonstr +  '*$*' + wfnode
  else if (dbm.gf.MobileWSFlag) and (jsonstr <> '') and (wfnode <> '')  then jsonstr := jsonstr +  ',' + wfnode;
end;

procedure  TASBDataObj.CreateMemVarNode;
  var memvarlist : string;
begin
  memvarlist := '';
  if act.CoreParser.MemVarList.Count > 0  then
  begin
    memvarlist := GetMemVars(act.CoreParser)
  end else if assigned(dbcall) then
  begin
    if dbcall.Parser.MemVarList.Count > 0 then
    begin
      memvarlist := GetMemVars(dbcall.Parser)
    end;
  end;
  if (dbm.gf.MobileWSFlag) and (jsonstr <> '') then jsonstr := Copy(trim(jsonstr),1,Length(trim(jsonstr))-1);
  if memvarlist <> '' then
  begin
     if dbm.gf.MobileWSFlag then
     begin
       memvarlist := '"memvar":[' + memvarlist +  ']';
       if jsonstr <> '' then jsonstr := jsonstr + ',' +  memvarlist
       else jsonstr := '{'+memvarlist+'}';
     end
     else
     begin
       memvarlist := '{"memvar":[' + memvarlist +  ']}';
       if jsonstr <> '' then jsonstr := jsonstr + '*$*' +  memvarlist
       else jsonstr := jsonstr + memvarlist ;
     end;
  end;
end;

function TASBDataObj.GetMemVars(parser : TProfitEval) : String;
  var i : integer;
      s,n,v,d : string;
begin
  result := '';
  s := '';
  for i := 0 to parser.MemVarList.Count - 1 do
  begin
    n := parser.MemVarList.Names[i];
    v := parser.MemVarList.Values[n];
    d :=  copy(parser.MemTypeStr,i+1,1);
    if pos(#$A,v) > 0  then v := dbm.gf.FindAndReplace(v,#$A,'<br>');
    if pos(#$D,v) > 0  then v := dbm.gf.FindAndReplace(v,#$D,'<br>');
    v:=dbm.gf.FindAndReplace(v, '"', '^^dq');
    s := s + '{"n":"'+n+'","v":"'+v+'"'+ ',"t":"'+d+'"},';
  end;
  if s <> '' then
  begin
    delete(s,length(s),1);
    result := s;
  end;
end;

function TASBDataObj.SaveFromAction : String;
var f,v,s,tcstr,tname,iname,recid:String;
    i : integer;
    notify : boolean;
    fld : pFld;
begin
    dbm.gf.dodebug.msg('Executing save from action');
    if not assigned(dbCall) then Exception.create('DBCalll not assigned for save action');
    dbm.gf.dodebug.msg('DBCalll assigned for save action');
    tcstr := CheckForAccess(dbcall.transid);
    if tcstr = '' then tcstr := 'd';
    if (tcstr = 'e') or (tcstr = 'd') then begin
      dbm.gf.dodebug.msg('Starting database transaction for validating & saving');
      try
        notify := dbcall.StoreData.ClientNotify;
        dbcall.XMLData := XMLDoc;
        dbcall.StoreData.ClientNotify := false;
        dbm.gf.dodebug.msg('DB Call Validate and save');
        dbcall.ClearFields := false;
        dbcall.Validate.OnWebSave := true;
        dbcall.Validate.ForIviewDef := IsForIviewDef(XMLDoc);
        if dbcall.Validate.ForIviewDef then
          dbcall.Validate.ivParamList := ivParamList;
        if dbcall.ValidateData = '' then begin
          AxProvider.DeleteFromTransCheck;
          dbm.StartTransaction(connection.ConnectionName);
          dbcall.SaveData;
        end;
        if dbcall.errorstr <> '' then begin
//          dbm.RollBack(connection.ConnectionName);
//          dbm.update_errorlog(dbcall.transid,dbcall.ErrorStr);
          act.ValidationErrorInAction := true;
          raise exception.Create(dbcall.ErrorStr);
        end else begin
          f:=dbcall.storedata.primarytablename+'id';
          v:=floattostr(dbcall.LastSavedId);
          dbm.gf.dodebug.msg('FNAme  ' + f);
          dbm.gf.dodebug.msg('Value  ' + v);
          //recid := floattostr(dbcall.GetRecordId(f,v));
          recid := v;
          dbm.gf.dodebug.msg('Record ID ' + recid);
          act.StoreData.LastSavedRecordId := strtofloat(recid);
          act.RecId := recid;
          result :=  recid + 'Transaction commited';
          {if dbcall.struct.HasImage then
          begin
            for i:=0 to dbcall.struct.flds.count-1 do begin
              fld := pfld(dbcall.struct.flds[i]);
              tname := '';
              iname := '';
              if fld.DataType = 'i' then
              begin
                if trim(dbcall.struct.SchemaName) <> '' then
                  tname := dbcall.struct.SchemaName + '.' + dbcall.transid+trim(fld.FieldName)
                else
                  tname := dbcall.transid+trim(fld.FieldName);
                f := fld.FieldName;
                iname := dbcall.Parser.GetVarValue(f);
                if (tname <> '') and (iname <> '') then
                begin
                  if not axprovider.SaveImage(tname,iname,strtofloat(recid)) then
                  begin
                     raise exception.Create('Could not save image...');
                  end;
                end;
              end;
            end;
          end;}
          //dbm.Commit(connection.ConnectionName);
          //dbm.gf.dodebug.msg('Transaction commited');
          dbcall.StoreData.ClientNotify := notify;
          if DBCall.cmdnode <> '' then act.cmdnode := DBCall.cmdnode;
          act.createcmdnode('action','save');
          act.cmdnode := act.cmdnode + '},';
          act.createcmdnode('recid',recid);
          act.cmdnode := act.cmdnode + '},';
          if FromScripts then
          begin
            SaveNodeFromScripts := act.cmdnode;
            act.cmdnode := '';
          end;
          v := '';
          if dbcall.AutoGenString <> '' then begin
            v := dbCall.struct.Caption +  ' Saved (' + dbcall.AutoGenString + ')'
          end else begin
            v := (dbCall.struct.Caption +  ' Saved')
          end;
          v := v + ',' + 'recordid=' + floattostr(dbcall.LastSavedId) ;
          act.createmsgnode(v);
          act.SavedFromAction := true;
        end;
      except on e:exception do
      begin
        dbm.gf.DoDebug.msg('Action Save');
        dbm.gf.DoDebug.msg('uASBDataObj\SaveFromAction\ Exception '+e.Message);
        act.ValidationErrorInAction := true;
        act.errorinactionsave := true;
        dbm.gf.DoDebug.msg('uASBDataObj\SaveFromAction\ Checking InTransaction ');
        if dbm.InTransaction then
        begin
           dbm.gf.DoDebug.msg('uASBDataObj\SaveFromAction\ InTransaction - True ');
           dbm.gf.DoDebug.msg('uASBDataObj\SaveFromAction\ RollBack ');
           //dbm.RollBack(connection.ConnectionName);
           s := dbm.GetAxpertMsg(e.Message);
        end else if dbcall.Validate.error_fieldname <> '' then s := e.Message+'" , "errfld" : "' + dbcall.Validate.error_fieldname + ',' + dbcall.Validate.error_row
        else begin
          s := e.Message;
          dbm.RollBack(connection.ConnectionName);
        end;
        dbm.gf.dodebug.msg(s);
        //dbm.update_errorlog(dbcall.transid,dbcall.ErrorStr);
        raise Exception.create(s);
      end;
      end;
    end else raise Exception.create('Access denied for saving this transaction');
end;

function TASBDataObj.LoadDataInAction(s : String ; n : ixmlNode) : String ;
var  tcstr, recid,dbtid:String;
     bkdbc:TDBCall;
begin
    result := '';
    dbtid := '';
    dbm.gf.dodebug.msg('Executing load from action');
    if not assigned(dbCall) then
    begin
      dbcall := TDbCall.Create;
      dbcaLL.axprovider := Axprovider;
      dbcall.Dbm := dbm;
      dbcall.transid := s;
      bkdbc := nil;
    end else
    begin
      bkdbc:=dbcall;
      dbtid := dbcall.transid;
      if s <> dbcall.transid then begin
        dbcall := TDbCall.Create;
        dbcall.axprovider := Axprovider;
        dbcall.Dbm := dbm;
        dbcall.transid := s;
        act.Moved_To_Other_Page := true;
      end;
    end;
    tcstr := '';
    tcstr := CheckForAccess(s);
    if (tcstr <> 'n') then begin
       recid := LoadDataForAction(s ,n );
       act.CoreParser.RegisterVar('recordid','c',recid);
       if (not act.Moved_To_Other_Page) and (actStructType <> 'iviews') and (recid <> '') then
       begin
         dbm.gf.dodebug.msg('Loading data from '+recid);
         if dbcall.struct.quickload then
         begin
            dbcall.LoadDataForWeb(strtofloat(recid)) ;
            act.RecId := recid;
            LoadDataByAction := true;
         end else dbcall.LoadData(strtofloat(recid));
       end;
    end else raise Exception.create('Access denied for this transaction');
    if (assigned(bkdbc)) and (bkdbc.transid <> s) then begin
      //FreeAndNil(dbCall);
      dbcall:=bkdbc;
      dbm.gf.dodebug.msg('dbcall reassigned');
    end;
    dbm.gf.dodebug.msg('Executing load from action over');
    result := dbtid;
end;

function TASBDataObj.LoadDataForAction(transid : String ;hypernode : ixmlNode) : String;
var ws,s,nv:String;
    x:txds;
    i:integer;
    xdoc: ixmldocument;
    n,n1 : IXMLNode;
begin
  dbm.gf.doDebug.Msg('Load Data Function');
  ws := '';
  result := '';
  if assigned(hypernode) and (hypernode.ChildNodes.Count>0) then begin
    for i:=0 to hypernode.childnodes.count-1 do begin
      nv := vartostr(hypernode.ChildNodes[i].Attributes['n']) ;
      if pos('.',nv) > 0 then
      begin
        if copy(nv,1,pos('.',nv)-1) <> transid then continue;
        delete(nv,1,pos('.',nv));
      end;

      if lowercase(nv) = 'recordid' then nv :=  dbCall.struct.PrimaryTable+'id';

      s:=vartostr(hypernode.ChildNodes[i].Attributes['v']);
      if copy(s,1,1) = ':' then begin
         delete(s,1,1);
         s := act.GetPValue(s);
      end;
      if s<>'' then
         ws:=ws + ' and '+nv+' = '+s;
    end;
    dbm.gf.doDebug.Msg('Where Condition : ' + ws);
  end;
  if ws<>'' then begin
    delete(ws, 1, 5);
    ws:='select ' + dbCall.struct.PrimaryTable+'id' + ' from '+ dbCall.struct.PrimaryTable+' where '+ws;
    x := axprovider.dbm.GetXDS(nil);
    x.buffered := True;
    x.CDS.CommandText:=ws;
    dbm.gf.doDebug.Msg('SQL : ' + ws);
    try
      xdoc:=axprovider.GetOneRecord(x.cds.CommandText, '', '');
    except on e : exception do
    begin
       x.close;
       ws := dbm.gf.FindAndReplace(ws,'=','like');  // to deal hyperlink from a text datatype field/column
       x.CDS.CommandText:=ws;
       xdoc:=axprovider.GetOneRecord(x.cds.CommandText, '', '');
    end;
    end;
    if xdoc.DocumentElement.childnodes.count>0 then
    begin
      n := xdoc.DocumentElement.ChildNodes[0];
      if n <> nil then n1 := n.ChildNodes.findnode(uppercase(dbCall.struct.PrimaryTable+'id'));
      if n1 = nil then n1 := n.ChildNodes.findnode(lowercase(dbCall.struct.PrimaryTable)+'id');
      if n1 = nil then n1 := n.ChildNodes.findnode(dbCall.struct.PrimaryTable+'id');
      if n1 <> nil then
      begin
        result := vartostr(n1.NodeValue);
        dbm.gf.doDebug.Msg('Record to load : ' + result);
      end;
    end;
    x.close;
    x.free;
  end;
end;

function TASBDataObj.OpenDataInAction(s : String ; rnode : ixmlNode) : String ;
var  tcstr,tid,v,f,dt:String;
    i,j,rowno:integer;
    fl : TStringList;
    fld : pFld;
begin
    dbm.gf.dodebug.msg('Executing Open TSruct from action');
    dbm.gf.dodebug.msg('Creating dbcall');
    fl := TStringList.Create;
    if not assigned(dbCall) then
    begin
      dbcall := TDbCall.Create;
      dbcaLL.axprovider := Axprovider;
      dbcall.Dbm := dbm;
    end;
    tid := s;
    dbcall.transid := tid;
    tcstr := CheckForAccess(dbcall.transid);
    for i:=0 to rnode.childnodes.count-1 do begin
      v:=vartostr(rnode.ChildNodes[i].Attributes['v']);
      if copy(v,1,1) = ':' then delete(v,1,1);
      f := vartostr(rnode.ChildNodes[i].Attributes['n']);
      v := act.coreparser.getvarvalue(v);
      fld:=dbcall.struct.GetField(f);
      if not assigned(fld) then continue;
      dt := fld.DataType;
      dbm.gf.dodebug.msg('Submiting values : ' + f+'='+v);
      dbcall.SubmitValue(f, v, '', 0, 0, 0);
      fl.Add(f);
    end;
    fl.Sort;
    for j:=0 to dbcall.struct.flds.Count -1 do begin
      fld := pfld(dbcall.struct.flds[j]);
      f := fld.FieldName;
      if fl.Find(f,i) then continue;
      if fld.AsGrid then continue;
      if fld.ModeofEntry = 'select' then continue;
      rowno := dbcall.StoreData.GetRowCount(fld.FrameNo);
      try
        dbcall.Validate.EnterField(f,rowno);
      except
        continue;
      end;
      v := dbcall.StoreData.GetFieldValue(f,rowno);
      if v = '' then continue;
      dbcall.Parser.RegisterVar(f,dt[1],v);
      dbm.gf.dodebug.msg('Register Var : '+f+'='+v);
      result := result + f + '=' + v + ',';
      act.flist.Add(f);
    end;
end;

function TASBDataObj.PageDataInAction(s : String ; rnode : ixmlnode) : String ;
var  tid,v,f,dt,sname :String;
    i,j,rowno,ns:integer;
    fl : TStringList;
    fld : pFld;
begin
    dbm.gf.dodebug.msg('Executing Page Data load from action');
    dbm.gf.dodebug.msg('Creating dbcall');
    fl := TStringList.Create;
    if not assigned(dbCall) then
    begin
      dbcall := TDbCall.Create;
      dbcaLL.axprovider := Axprovider;
      dbcall.Dbm := dbm;
    end;
    ns:=1;
    while true do begin
      tid:=dbm.gf.getnthstring(s,ns);
      if tid='' then break;
      dbcall.transid := tid;
      CheckForAccess(dbcall.transid);
      for i:=0 to rnode.childnodes.count-1 do begin
        v:=vartostr(rnode.ChildNodes[i].Attributes['v']);
        if copy(v,1,1) = ':' then delete(v,1,1);
        f := vartostr(rnode.ChildNodes[i].Attributes['n']);
        sname := copy(f,1,pos('.',f)-1);
        dbm.gf.dodebug.msg('Sname in page : ' + sname);
        delete(f,1,pos('.',f));
        if tid <> sname then continue;
        v := act.coreparser.getvarvalue(v);
        fld:=dbcall.struct.GetField(f);
        if not assigned(fld) then continue;
        dt := fld.DataType;
        dbm.gf.dodebug.msg('Submiting values : '+f+'='+v);
        dbcall.SubmitValue(f, v, '', 0, 0, 0);
        fl.Add(f);
      end;
      fl.Sort;
      for j:=0 to dbcall.struct.flds.Count -1 do begin
        fld := pfld(dbcall.struct.flds[j]);
        f := fld.FieldName;
        if fl.Find(f,i) then continue;
        if fld.AsGrid then continue;
        if (fld.ModeofEntry = 'select') then continue;
        rowno := dbcall.StoreData.GetRowCount(fld.FrameNo);
        try
          dbcall.Validate.EnterField(f,rowno);
        except
          continue;
        end;
        v := dbcall.StoreData.GetFieldValue(f,rowno);
        if v = '' then continue;
        dbcall.Parser.RegisterVar(f,dt[1],v);
        dbm.gf.dodebug.msg('Register Var : '+f+'='+v);
        f := tid + '.' + f;
        result := result + f + '=' + v + ',';
        act.flist.Add(f);
      end;
      inc(ns);
    end;
    fl.Free;
end;

procedure TASBDataObj.SetValueInAction(n:IXMLNode);
var fn, Fv, r,s,v,oldval,fldList : String;
    i,rn,j,fno,k : Integer;
    flist : TStringList;
    fld : pFld;
begin
  dbm.gf.dodebug.Msg('Set Value Function In Action');
  rn := 0;
  fno := 0;
  i := n.ChildNodes.Count ;
  j := 0;
  fldList := ','+FormLoadFldList+',';
  flist := TStringList.Create;
  while i > 0 do begin
    dbm.gf.dodebug.Msg('Row No :' + inttostr(i));
    fn := vartostr(n.ChildNodes[j].NodeValue);
    j := j + 1 ;
    fv := vartostr(n.ChildNodes[j].NodeValue);
    if copy(fv,1,1) = ':' then
    begin
      delete(fv,1,1);
      fv := act.GetParamValue(fv);
    end;
    j := j + 1;
    r := vartostr(n.ChildNodes[j].NodeValue);
    if copy(r,1,1) = ':' then
       rn := strtoint(act.GetParamValue(r))
    else if r <> '' then
       rn := strtoint(r);
    j := j + 1;
    dbm.gf.dodebug.Msg('Field Name :' + fn);
    dbm.gf.dodebug.Msg('Field value :' + fv);
    dbCall.Storedata.SubmitValue(fn,rn, fv, '', 0, 0, 0);
    if pos(','+fn+',' , fldList) = 0 then
    begin
       if FormLoadFldList = '' then FormLoadFldList := fn
       else FormLoadFldList := FormLoadFldList + ',' + fn;
    end;
    fld:=dbcall.struct.GetField(fn);
    fno := fld.FrameNo;
    i := i - 3;
    if (fno > 1) and (rn > 1) then
       RegisterActiveRow(fno,rn)
    else
       dbcall.Parser.RegisterVar(fn,fld.DataType[1],fv);
    if (not FormLoadingAct) then GetDepentendFldList(fn,flist);
  end;
  dbm.gf.dodebug.Msg('Refreshing values in depend field');
  for i := 0 to flist.Count - 1 do
  begin
    s := flist.Strings[i];
    fld:=dbcall.struct.GetField(s);
    if not assigned(fld) then continue;
    if pos(','+s+',' , fldList) = 0 then FormLoadFldList := FormLoadFldList + ',' + s;
    if fld.txtSelection = true then
    begin
      if fld.Exprn > -1 then
      begin
        If DbCall.Parser.EvalPrepared(fld.Exprn) Then
          v := DbCall.Parser.Value;
        DbCall.StoreData.SubmitValue(fld.FieldName, 1, v, '', 0, 0, 0);
        DbCall.Parser.RegisterVar(fld.FieldName,fld.DataType[1],v);
      end ;
      continue;
    end;
    if not fld.AsGrid then
    begin
       v := DbCall.Validate.RefreshField(fld,1);
    end else begin
      j := dbcall.StoreData.GetRowCount(fld.FrameNo);
      if j > 0  then
      begin
        if (fno = fld.FrameNo) then
        begin
           v := DbCall.Validate.RefreshField(fld,1) ;
        end else
        begin
           for k := 1 to j do
           begin
             DbCall.Validate.RegRow(fld.FrameNo,k);
             oldval := DbCall.Validate.Parser.GetVarValue(fld.FieldName);
             v := DbCall.Validate.RefreshField(fld,k) ;
             if v <> oldval then UpdateChangedRowsList(fld.FrameNo,k);
           end;
        end;
      end;
    end;
  end;
  if assigned(flist) then flist.Free;
end;

Procedure TASBDataObj.DoFillInAction(n:IXMLNode);
var sl : TStringList;
    fxml : ixmldocument;
    fs : TFillStruct;
    i : Integer;
begin
  dbm.gf.dodebug.Msg('Do Fillvalues InAction');
  DataFilledDcs := '';
  if n.ChildNodes.Count=0 then exit;
  if not assigned(dbcall) then exit;
  n:=n.childnodes[0];
  sl := TStringList.Create;
  fs:=nil;
  try
    for i := 0 to n.ChildNodes.Count-1 do
      sl.Add(vartostr(n.ChildNodes[i].NodeValue));
    fxml := LoadXMLDataFromWS(sl.Text);
    fs := TFillStruct.Create;
    dbm.gf.dodebug.Msg('Fill Object created');
    fs.axp := axprovider;
    dbcall.Validate.Parser := dbCall.Parser;
    fs.validate := dbcall.Validate;
    fs.StoreData := dbcall.StoreData;
    if dbcall.StoreData.LastSavedRecordId = 0 then
       fs.NewTrans := true
    else fs.NewTrans := false;
    fs.ActionNode := fxml.DocumentElement;
    dbm.gf.dodebug.Msg('Calling fill function in Fill Object');
    fs.Fill;
    DataFilledDcs := fs.DCNames;
    fs.Destroy;
    fxml := nil;
    sl.clear;
    sl.Free;
  except on e:exception do
    begin
      fs.Destroy;
      fxml := nil;
      sl.clear;
      sl.Free;
      raise exception.Create(e.message);
    end;
  end;
end;

function TASBDataObj.DeleteDataInAction : String;
var  tcstr,tid:String;
    f : extended ;
begin
    dbm.gf.dodebug.msg('Executing Delete from action');
    if  act.transid = '' then raise exception.Create('Structure Name not defined for this action');
    if not assigned(dbCall) then
    begin
      dbm.gf.dodebug.msg('Creating dbcall');
      dbcall := TDbCall.Create;
      dbcaLL.axprovider := Axprovider;
      dbcall.Dbm := dbm;
      if act.stype = 'tstructs' then dbcall.transid := act.sName;
    end;
    tid := act.RecId;
    f := dbm.gf.strtofloatz(tid);
    dbm.gf.dodebug.msg(' Recordid='+floattostr(f));
    if f>0 then begin
       tcstr :=  CheckForAccess(dbcall.transid);
       if (tcstr = 'd') or (tcstr = '') then
       begin
          dbcall.LoadData(f);
          if dbcall.storedata.cancelled then
          begin
            act.createmsgnode('Can not delete the cancelled transaction');
            dbm.gf.dodebug.msg('Can not delete the cancelled transaction');
            exit;
          end;
          try
           dbm.StartTransaction(connection.ConnectionName);
           dbcall.DeleteData(f);
           if dbcall.ErrorStr <> '' then
              result := dbcall.ErrorStr
           else
           begin
              dbm.Commit(connection.ConnectionName);
              result := 'done';
           end;
          except on e:exception do
            begin
               dbm.RollBack(connection.ConnectionName);
               raise exception.Create(e.Message);
            end;
          end;
           act.createmsgnode('Transaction has been deleted');
       end else raise exception.Create('Access denied for deleting this transaction');
    end;
end;

function TASBDataObj.CancelDataInAction : String;
var  tcstr,tid,rem:String;
    f : extended ;
    rNode : ixmlnode;
begin
    dbm.gf.dodebug.msg('Executing CANCEL from action');
    if  act.transid = '' then raise exception.Create('Structure Name not defined for this action');
    if not assigned(dbCall) then
    begin
      dbm.gf.dodebug.msg('Creating dbcall');
      dbcall := TDbCall.Create;
      dbcaLL.axprovider := Axprovider;
      dbcall.Dbm := dbm;
      if act.stype = 'tstructs' then dbcall.transid := act.sName;
    end;
    tid := act.RecId;
    if tid = '' then tid := dbcall.parser.GetVarValue('recordid');
    f := dbm.gf.strtofloatz(tid);
    dbm.gf.dodebug.msg(' Recordid='+floattostr(f));
    if f>0 then begin
       tcstr :=  CheckForAccess(dbcall.transid);
       if (tcstr = 'd') or (tcstr = '') then
       begin
          dbcall.LoadData(f);
          if dbcall.storedata.cancelled then
          begin
            act.createmsgnode('Transaction already cancelled');
            dbm.gf.dodebug.msg('Transaction already cancelled');
            exit;
          end;
          try
           rem := '';
           rNode := xmldoc.DocumentElement.ChildNodes.FindNode('cancelremarks');
           if assigned(rnode) then
             rem:= VartoStr(rnode.NodeValue);
           dbm.StartTransaction(connection.ConnectionName);
           dbcall.CancelTransaction(f,rem);
           result := 'done';
           dbm.Commit(connection.ConnectionName);
          except on e:exception do
            begin
               dbm.RollBack(connection.ConnectionName);
               act.ValidationErrorInAction := true;
               raise exception.Create(e.Message);
            end;
          end;
          result := 'done';
          act.createmsgnode('Transaction has been canceled');
          //act.createcmdnode('cancel','cancel');
          //act.cmdnode := act.cmdnode + ',"reason":"'+'"';
          //act.cmdnode := act.cmdnode + '},';
       end else raise exception.Create('Access denied for deleting this transaction');
    end;
end;

function TASBDataObj.PostIviewFromAction(n:ixmlnode):String;
var x,f,v,c,dtype : String;
    i : Integer;
    cnode : IXMLNode;
begin
    result := '';
    try
      (*
      While posting (bulk post) from iview (with all selected rows), the dbcall object holds the parser data of the previous transaction.
      We were not destroying dbcall while processing each row.
      Due to that, we are getting issues.

      To handle this, we are destroying the dbcall object every time.
      NOTE: It has been handled now as same as in the Desktop application.
      ref:uDoAction-> PostIview
      *)
      if assigned(dbcall) then
        FreeAndNil(dbcall);

      dbcall := TDbCall.Create;
      dbcaLL.axprovider := Axprovider;
      dbcall.Dbm := dbm;

      x:= vartostr(n.ChildValues['name']);
      c:= vartostr(n.ChildValues['gcolumn']);
      dbcall.transid := x;
      n := n.ChildNodes.FindNode('fieldvalues');
      for i:=0 to n.ChildNodes.Count-1 do begin
        cnode := n.ChildNodes[i];
        f := vartostr(cnode.Attributes['n']);
        v := vartostr(cnode.Attributes['v']);
        if copy(v,1,1) = ':' then delete(v,1,1);
        v := act.Coreparser.GetVarValue(v);
        dtype := act.Coreparser.LastVarType;
        if dtype = '' then dtype := 'c';
        dbcall.SubmitValue(f, v, '', 0, 0, 0);
        dbcall.Parser.RegisterVar(f,dtype[1],v);
      end;
      try
        dbm.gf.DoDebug.msg('Starting database transaction for validating & saving');
        if not dbm.InTransaction then  dbm.StartTransaction(dbm.gf.connectionname);
        result:=dbcall.ValidateAndSave;
        if dbcall.errorstr <> '' then begin
          if dbm.InTransaction then dbm.RollBack(dbm.gf.connectionname);
          dbm.update_errorlog(dbcall.transid,dbcall.ErrorStr);
          dbm.gf.DoDebug.msg('Error : '+ dbcall.ErrorStr);
          act.createmsgnode(dbcall.ErrorStr);
  //        result := dbcall.errorstr;
  //        exit;
        end else begin
          if dbm.InTransaction then dbm.Commit(dbm.gf.connectionname);
          dbm.gf.DoDebug.msg('Transaction commited');
          if not msgnode_created then act.createmsgnode('Transaction commited');
          msgnode_created := true;
        end;
      except
        on e:exception do begin
  //        result:=e.message;
          act.createmsgnode(e.message);
          if dbm.InTransaction then dbm.RollBack(dbm.gf.ConnectionName);
          dbm.update_errorlog(dbcall.transid,e.Message);
  //        exit;
        end;
      end;
    finally
      if assigned(dbcall) then
        FreeAndNil(dbcall);
    end;
    result := 'done';
end;

Procedure TASBDataObj.CopyCSVToDCInAction(Filename, transname, hline,seperator:String; Csflist:TStringList);
var i,k, n, hl,fnp,fno,m,l,inx,row,p: integer;
    f:TextFile;
    ls,  rs,  fname, value, OneLine,s : String;
    fld,fd : pfld;
    enode : ixmlnode;
    fm:pfrm;
    fldname,fldval : TStringlist;
begin
  try
    dbm.gf.DoDebug.msg('Importing');
    IsImpFromCSVToDC := True;
    fm := nil;
    fldname := TStringlist.create;
    fldval := TStringlist.create;
    enode := dbcall.struct.XML.DocumentElement;
    fno := 0;
    hl := 0;
    row := 1;
    AssignFile(f, filename);
    reset(f);
    While not eof(f) do begin
      Readln(f, OneLine);
      if trim(oneline)='' then continue;
      dbm.gf.DoDebug.msg(oneline);
      if (hl = 0) and (hline = 'hline') then
      begin
         hl := 1;
         continue;
      end;
      for i:=0 to Csflist.count-1 do begin
        if pos('=', csflist[i]) >0 then begin
          n:=pos('=', csflist[i]);
          ls := trim(Copy(csflist[i],1,n-1));
          rs := trim(Copy(csflist[i],n+1, Length(csflist[i])));
        end
        else ls := csflist[i];
        fname := dbm.gf.getnthstring(ls,1);
        if fno = 0 then begin
           fld := dbcall.struct.GetField(fname);
           if assigned(fld) then begin
            dbcall.fnames.Clear;
            fno := fld.FrameNo;
            fnp := DbCall.storedata.GetRowCount(fno);
            dbm.gf.dodebug.msg('Frame No : ' + inttostr(fno));
            dbm.gf.dodebug.msg('Row No : ' + inttostr(fnp));
            for m:=1 to fnp do
              DbCall.StoreData.DeleteRow(fno,1);
          end;
        end;
        if rs[1] = '+' then begin
          DbCall.Parser.Evaluate(copy(rs,2,length(rs)));
          Value := DbCall.Parser.value;
        end else begin
          k := StrToInt(trim(copy(rs,5,length(rs))));
          Value := GetDataSimple(OneLine,seperator,k)
        end;
        fname := lowercase(fname);
        fldname.Add(fname) ;
        fldval.Add(value);
      end;
      if fm = nil then    fm:=dbcall.Struct.GetFrame(fno);
      for l := fm.StartIndex to fm.StartIndex+fm.FieldCount-1 do
      begin
        fd:=pfld(DbCall.struct.flds[l]);
        inx := fldname.IndexOf(lowercase(fd.FieldName));
        if inx  > -1 then
        begin
          value := fldval.Strings[inx];
          DbCall.StoreData.SubmitValue(fd.FieldName, row , value, '', 0, 0, 0);
          dbcall.Parser.RegisterVar(fd.FieldName,fd.DataType[1],value);
          if fd.SourceKey then
            DbCall.validate.RefreshField(fd, row);
        end else
        begin
          dbcall.validate.RefreshField(fd, row)
        end;
        p := dbcall.PopAtThisField(fd);
        if p <> -1 then
           dbcall.DoPopUp(p, row);
      end;
      inc(row);
      dbm.gf.dodebug.msg('Row No : ' + inttostr(row) + ' refreshing over');
      fldname.Clear;
      fldval.Clear;
    end;
  except on e:exception do
  begin
     s := e.Message;
     dbm.gf.dodebug.msg('Error in Grid Import: ' + s);
  end;
  end;
  CloseFile(f);
  fldname.free;
  fldval.free;
end;

Function TASBDataObj.GetDataSimple(OneLine,seperator:String; k:integer):String;
var s,s1:String;
    i, p, j :integer;
begin
  p:=0;
  j:=0;
  s := oneline+seperator;
  s1:='';
  for i:= 1 to Length(s) do begin
    if s[i] = seperator then inc(p);
       if p = k then
       begin
         j :=i;
         break;
       end;
  end;
  s := copy(s,1,j-1);
  for i:= length(s) downto 1 do begin
    if s[i] = seperator then begin
         break;
    end
    else begin
        s1 := s[i]+ s1;
       end;
  end;
result := s1;
end;

procedure TASBDataObj.RegisterActiveRow(fno,rno : integer);
  var j : integer;
    f,v,dt:String;
    fld : pFld;
begin
  dbm.gf.dodebug.msg('Registering Active row values');
  for j:=0 to dbcall.struct.flds.Count -1 do begin
    fld := pfld(dbcall.struct.flds[j]);
    if fno = fld.FrameNo then
    begin
      f := fld.FieldName;
      dt := fld.DataType;
      v := dbcall.StoreData.GetFieldValue(f,rno);
      dbcall.Parser.RegisterVar(f,dt[1],v);
      dbm.gf.dodebug.msg('Register Var : '+f+'='+v);
    end;
  end;
  dbm.gf.dodebug.msg('Registering Active row values completed');
end;

procedure TASBDataObj.GetDepentendFldList(fname : String ; flist : TStringList);
  var i,j, l,k : integer;
      n,dnode : ixmlnode;
      dlist,s,s1,s2 : String;
      fd:pfld;
begin
  n :=dbcall.struct.XML.DocumentElement.ChildNodes.FindNode(fname);
  if n <> nil then dnode := n.ChildNodes.FindNode('a58');
  if dnode <> nil then
  begin
    if vartostr(dnode.NodeValue) <> ''then
    begin
      dlist := VarToStr(dnode.NodeValue) + ',';
      i := 1;
      while true do
      begin
        s := axprovider.dbm.gf.GetnthString(dlist,i);
        if s = '' then break;
        inc(i);
        if copy(s,1,1) = 'd' then s := fname + '$' + s
        else delete(s,1,1);
        if flist.IndexOf(s) < 0 then flist.Add(s);
      end;
    end ;
    dbm.gf.dodebug.msg('Dep full list : ' + flist.Text);
  end;
end;

function TASBDataObj.LoadXMLDataFromWS(xml : WideString) : IXMLDocument;
begin
   result := LoadXMLData(xml);
end;

//ProcessandPopulateDeletedRows - populates empty field since this will not be sent from ARM | for paybooks
procedure TASBDataObj.ProcessandPopulateDeletedRows(delrowsNode: IXMLNode);
var
  i, j, k: Integer;
  dcNode, recidNode, insertAfterNode, newFldNode, templateFldNode: IXMLNode;
  dcno: Integer;
  fm: pFrm;
  fldVal, fldNodeName: string;
  fld: pFld;
  fldnode: IXMLNode;
begin
  // Loop through all children of <delrows>
  for i := 0 to delrowsNode.ChildNodes.Count - 1 do
  begin
    dcNode := delrowsNode.ChildNodes[i];

    // Ensure it's a dc node
    if not dcNode.IsTextElement and (Pos('dc', dcNode.NodeName) = 1) then
    begin
      // Extract dc number, e.g., dc2 => 2
      dcno := StrToIntDef(Copy(dcNode.NodeName, 3, MaxInt), -1);
      if dcno = -1 then Continue;

      // Get corresponding pFrm structure
      fm := pFrm(dbcall.struct.Frames[dcno - 1]);

      // Create a template fldnode (will not be added to XML directly)
      fldnode := delrowsNode.OwnerDocument.CreateNode('tempflds');

      // Prepare field nodes based on StructDef
      for j := fm.StartIndex to fm.StartIndex + fm.FieldCount - 1 do
      begin
        fld := pFld(dbcall.struct.flds[j]);

        // Create node name like fld21, fld22, etc.
        fldNodeName := fld.Fieldname; //Format('fld%d%d', [dcno, j - fm.StartIndex + 1]);

        if fld.DataType = 'n' then
          fldVal := '0'
        else
          fldVal := '';

        // Add to template fldnode
        fldnode.AddChild(fldNodeName).Text := fldVal;
      end;

      // Now insert copies of fldnode’s children after each axp_recidX node
      j := 0;
      while j < dcNode.ChildNodes.Count do
      begin
        recidNode := dcNode.ChildNodes[j];

        if Pos('axp_recid', recidNode.NodeName) = 1 then
        begin
          Inc(j); // Move index to insert after current recidNode

          // Insert cloned fld nodes
          for k := 0 to fldnode.ChildNodes.Count - 1 do
          begin
            templateFldNode := fldnode.ChildNodes[k];
            newFldNode := dcNode.AddChild(templateFldNode.NodeName, j);
            newFldNode.Text := templateFldNode.Text;
            Inc(j); // Advance j to account for inserted node
          end;
        end
        else
          Inc(j); // Skip unrelated nodes
      end;
    end;
  end;
end;


procedure TASBDataObj.RemoveDeletedWebRowsFromStoreDate;
   var delrowsxml : ixmldocument;
       delrowsnode : ixmlnode;
begin
    if XMLDoc.DocumentElement.ChildNodes.FindNode('delrows') <> nil then
    begin
      delrowsxml := LoadXMLData('<root></root>');
      delrowsxml.DocumentElement := XMLDoc.DocumentElement.ChildNodes.FindNode('delrows').CloneNode(true);
      XMLDoc.DocumentElement.ChildNodes.Delete('delrows');
      delrowsnode := delrowsxml.DocumentElement;
    end;
    if assigned(delrowsnode) then
    begin
       if delrowsnode.HasChildNodes then
       begin
        if dbcall.bForPayBooks then ProcessandPopulateDeletedRows(delrowsnode); //For paybooks as they will not send fields for deleted rows
        DeleteRowsFromStoreData(delrowsnode);
       end;
    end;
end;

//SafeStrToInt
function SafeStrToInt(const S: string; const Default: Integer = 1): Integer;
begin
  if not TryStrToInt(S, Result) then
    Result := Default;
end;

procedure TASBDataObj.DeleteRowsFromStoreData(delrowsnode : ixmlnode);
  var i,j,k,fno,count, delrow, delcount : integer;
      f,v,s,fn : String;
      n,n1 : ixmlnode;
      recid : extended;
      fld : pFld;

      //handling deleterow 'axpfile_' attachments
      axpimgserver, axpimgpath, axpfilepath, delFilename : String;
      slAttachmentList : TStringList;
      sDeletedrno : String;

      // DeleteAxpFileAttach
      Procedure DeleteAxpFileAttach(pAxpFileAttachList : TStringList);
      var
        fldName, fldValue, filepathfldname, filepath,
          filename: String;
        iIdx, iJdx, filepathIdx, ifldValCount: integer;
      begin
        try
          dbm.gf.DoDebug.Msg('DeleteAxpFileAttach starts...');
          try
            if Not Assigned(pAxpFileAttachList) or (pAxpFileAttachList.Count = 0) then
              Exit;

            dbm.gf.DoDebug.Msg('Executing PrepareAxpFileAttachList...');
            begin
              pAxpFileAttachList.Delimiter := '|';
              pAxpFileAttachList.StrictDelimiter := True;
              dbm.gf.DoDebug.Msg('Processing AxpFileAttachList...');
              For iIdx := 0 to pAxpFileAttachList.Count - 1 do
              begin
                fldName := pAxpFileAttachList.Names[iIdx];

                if AnsiStartsStr('axpfilepath_',lowercase(fldName)) then
                  Continue; //process only axpfile_ field , path foield can be used when fecthing path value

                fldValue := pAxpFileAttachList.ValueFromIndex[iIdx];

                dbm.gf.DoDebug.Msg('processing field :' + fldName);
                dbm.gf.DoDebug.Msg('fieldvalue :' + fldValue);

                // preparing axpfilepath_ fieldname
                filepathfldname := 'axpfilepath_' + dbm.gf.GetNthString(fldName, 2,'_'); // filepath fldname   - fldname+'$'+rowno - this is how the name stored in SList
                // Checking the existense of axpfilepath_ fieldname
                filepathIdx := pAxpFileAttachList.IndexOfName(filepathfldname);
                if filepathIdx = -1 then // filepath fld doesn''t exists
                  filepath := axpfilepath
                  // if filepath doesn''t exists, assign global var value filepath
                else
                begin
                  filepath := pAxpFileAttachList.ValueFromIndex[filepathIdx];
                   //delete '*' from file path | this is used for adding timstamp prefix to filenames
                  if AnsiEndsStr('*',filepath) then
                    Delete(filepath,length(filepath),1);
                end;
                filepath := IncludeTrailingBackslash(filepath);
                dbm.gf.DoDebug.Msg('filepath :' + filepath);
                if Trim(fldValue) <> '' then
                begin
                  // axpfile_ attach field accepts mutiple files, in comma separated format
                  ifldValCount := dbm.gf.CountNumberOfComma(fldValue);
                  iJdx := 1;
                  filename := dbm.gf.GetNthString(fldValue, iJdx);
                  While filename <> '' do
                  begin
                    dbm.gf.DoDebug.Msg('filename :' + filename);
                    delFilename := filepath + filename;
                    if FileExists(delFilename) then
                    begin
                      dbm.gf.DoDebug.Msg('Deleting file: ' + delFilename);
                      DeleteFile(delFilename);
                      if FileExists(delFilename) then
                        dbm.gf.DoDebug.Msg('File not deleted.');
                    end
                    else
                    begin
                      // Handle the case when the file doesn't exist
                      raise Exception.Create('File does not exist: ' + delFilename);
                    end;
                    Inc(iJdx);
                    filename := dbm.gf.GetNthString(fldValue, iJdx);
                  end;
                end;

              end;
              dbm.gf.DoDebug.Msg('Processing DeleteAxpFileAttach ends.');
            end;
          except
            on E: Exception do
            begin
              dbm.gf.DoDebug.Msg('Error in DeleteAxpFileAttach : ' +
                E.message);
            end;
          end;
        finally

        end;
        dbm.gf.DoDebug.Msg('DeleteAxpFileAttach ends.');
      end;

begin
  dbm.gf.dodebug.msg('Setting deleted rows in storedata');
  slAttachmentList := nil;
  try
  slAttachmentList := TStringList.Create;
  slAttachmentList.Delimiter := '|';
  slAttachmentList.StrictDelimiter := True;
  if Assigned(dbCall.Parser) then
  begin
    axpimgserver := Trim(dbCall.Parser.getvarvalue('axpimageserver'));
    if axpimgserver <> '' then
      axpimgserver := IncludeTrailingBackslash(axpimgserver);
    axpimgpath := Trim(dbCall.Parser.getvarvalue('axpimagepath'));
    if axpimgpath <> '' then
      axpimgpath := IncludeTrailingBackslash(axpimgpath);
    axpfilepath := axpimgserver + axpimgpath;
  end;

  //only for rest we are doing this so added skipMaxrows (bForPayBooks) condition
  //name can be changed if needed.
  //and for soap we letting it work as it's

  if dbcall.bForPayBooks and delrowsnode.HasAttribute('deletedrno') then
    sDeletedrno := vartostr(delrowsnode.Attributes['deletedrno']);
  delcount := 0;
  for i := 0 to delrowsnode.ChildNodes.Count - 1 do
  begin
    n := delrowsnode.ChildNodes[i];
    fn := n.NodeName;
    delete(fn,1,2);
    fno := dbm.gf.strtointz(fn);
    count := 0;
    for k := 0 to n.ChildNodes.Count - 1 do
    begin
      n1 := n.ChildNodes[k];
      f := vartostr(n1.NodeName);
      v := vartostr(n1.NodeValue);
      if copy(f,1,9) = 'axp_recid' then  begin
         recid := dbm.gf.strtofloatz(v);
         if count > 0 then
         begin
          if Not dbcall.bForPayBooks then
            dbcall.storedata.DeleteRow(fno,1)
          else
            dbcall.storedata.DeleteRow(fno,delrow);
          //For each row attachment lsit will be prepared and attament will e deleted.
          DeleteAxpFileAttach(slAttachmentList);
          slAttachmentList.Clear;
         end;
         if dbcall.bForPayBooks then
         begin
           inc(delcount);
           //assigning delrow must be here , so for every recid delete it finds corresponding delrowno
           delrow := SafeStrToInt(dbm.gf.getNthString(sDeletedrno,delcount));
         end;
         inc(count);
         continue;
      end;
      fld:=dbcall.struct.GetField(f);
      if assigned(fld) then
      begin
         //if it's an attachment / attachmentpath field
         if AnsiStartsStr('axpfile_',f) or AnsiStartsStr('axpfilepath_',f) then
         begin
          //add attachment path and value to sl list for the curr row / till axp_recid change
          slAttachmentList.Add(f+'='+v);
         end;

        if Not dbcall.bForPayBooks then
          dbcall.storedata.SubmitValue(fld,fld.fieldname, 1 , '' , v, 0, 0, recid)
        else
          dbcall.storedata.SubmitValue(fld,fld.fieldname, delrow , '' , v, 0, 0, recid);
      end;
    end;
    if count > 0 then
    begin
      if Not dbcall.bForPayBooks then
        dbcall.storedata.DeleteRow(fno,1)
      else
        dbcall.storedata.DeleteRow(fno,delrow);
      DeleteAxpFileAttach(slAttachmentList);
      slAttachmentList.Clear;
    end;
  end;
  finally
    if Assigned(slAttachmentList) then
       FreeAndNil(slAttachmentList);
  end;
end;

procedure TASBDataObj.CreateChangedRowsList;
   var chgrowsxml : ixmldocument;
       chgrowsnode : ixmlnode;
begin
    if XMLDoc.DocumentElement.ChildNodes.FindNode('changedrows') <> nil then
    begin
      chgrowsxml := LoadXMLData('<root></root>');
      chgrowsxml.DocumentElement := XMLDoc.DocumentElement.ChildNodes.FindNode('changedrows').CloneNode(true);
      XMLDoc.DocumentElement.ChildNodes.Delete('changedrows');
      chgrowsnode := chgrowsxml.DocumentElement;
    end;
    if assigned(chgrowsnode) then
    begin
       if chgrowsnode.HasChildNodes then ChangedWebRowsList(chgrowsnode);
    end;
end;

procedure TASBDataObj.ChangedWebRowsList(delrowsnode : ixmlnode);
  var i,k : integer;
      f,v,s,rno : String;
      n : ixmlnode;
begin
  dbm.gf.dodebug.msg('Creating changed web rows list');
  for i := 0 to delrowsnode.ChildNodes.Count - 1 do
  begin
    n := delrowsnode.ChildNodes[i];
    f := trim(vartostr(n.NodeName));
    v := trim(vartostr(n.NodeValue));
    dbcall.Validate.chgrowdcs.Add(f);
    rno := '';
    k := 1;
    while True do
    begin
      s := dbm.gf.GetNthString(v,k);
      if s = '' then break;
      inc(k);
      rno := rno + quotedstr(s) +',' ;
    end;
    dbm.gf.dodebug.msg('Changed Web DC : ' + f);
    dbm.gf.dodebug.msg('Changed Web DC Rows: ' + rno);
    dbcall.Validate.chgrownos.Add(rno);
  end;
end;

procedure TASBDataObj.UpdateChangedRowsList(fno,rno : integer);
  var chgdcs : integer;
  s : String;
begin
  dbm.gf.dodebug.msg('Updating changed web rows list');
  dbm.gf.dodebug.msg('Changed Web DC : ' + inttostr(fno));
  dbm.gf.dodebug.msg('Changed Web DC Rows: ' + inttostr(rno));
  s := 'dc'+inttostr(fno) ;
  chgdcs := dbcall.Validate.chgrowdcs.IndexOf(s);
  if chgdcs >= 0 then
  begin
    dbcall.Validate.chgrownos.Strings[chgdcs] := dbcall.Validate.chgrownos.Strings[chgdcs] + quotedstr(inttostr(rno)) + ',';
  end else begin
    dbcall.Validate.chgrowdcs.Add('dc'+inttostr(fno));
    dbcall.Validate.chgrownos.Add(quotedstr(inttostr(rno)));
  end;
end;

//sab functions for making JSON

function TASBDataObj.GetJSON  : String  ;
Var
  bFlag : Boolean;
begin
   bFlag := False;
   if (dbm.gf.MobileWSFlag) and (jsonstr <> '') then bFlag := true;
   try
     if MakeMemVarNode then CreateMemVarNode;
     if not dbm.gf.MobileWSFlag then
     begin
       CreateFormControlNode ;
       CreateCommandNode;
     end;
     CreateMessageNode;
     if ((dbm.gf.MobileWSFlag) and (ServiceName <> 'Saving data')) or (not dbm.gf.MobileWSFlag) then
     CreateAttachmentNode;
     CreateWorkflowNode;
   except
   end;
   if bFlag then
     result := jsonstr + '}'
   else
   result := jsonstr;
   if Assigned(ASBCommonObj) and (ASBCommonObj.enableAPILogging) then
    ASBCommonObj.restApiResponse := result;
end;

procedure TASBDataObj.GetAxpRecid(frameno, rowno:integer);
var j, fno,rno:integer;
    recid:extended;
    fm:pFrm;
begin
  Recid:=dbcall.storedata.GetParentDocId(FrameNo, Rowno);
  fm:=dbcall.struct.GetFrame(frameno);
  if not fm.AsGrid then rno:=0 else rno:=rowno;
  fm.jsonstr := fm.jsonstr + '{"n":"'+'axp_recid'+inttostr(frameno)+'","v":"'+floattostr(recid)+'","r":"'+inttostr(rno)+'","t":"s"},';
end;

procedure TASBDataObj.GetAxpRecid(FrameNo:integer);
var i,rcount :integer;
    fm:pFrm;
    recid:extended;
begin
  fm:=dbcall.struct.GetFrame(frameno);
  if not fm.AsGrid then begin
    recid:=dbcall.StoreData.GetParentDocId(fm.FrameNo,1);
    fm.jsonstr := fm.jsonstr + '{"n":"'+'axp_recid'+inttostr(fm.frameno)+'","v":"'+floattostr(recid)+'","r":"'+'0'+'","t":"s"},';
  end else begin
    rcount:=dbcall.storedata.GetRowCount(fm.FrameNo);
    for I := 1 to rcount do begin
      if (existing_gridrows > 0)  and (i <= existing_gridrows) then continue;
      recid:=dbcall.StoreData.GetParentDocId(fm.FrameNo,i);
      fm.jsonstr:=fm.jsonstr + '{"n":"'+'axp_recid'+inttostr(fm.frameno)+'","v":"'+floattostr(recid)+'","r":"'+inttostr(i-existing_gridrows)+'","t":"s"},';
    end;
  end;
end;

procedure TASBDataObj.GetAxpRecid(fm:pFrm);
var i,rcount :integer;

    recid:extended;
begin
  if not fm.AsGrid then begin
    recid:=dbcall.StoreData.GetParentDocId(fm.FrameNo,1);
    fm.jsonstr := fm.jsonstr + '{"n":"'+'axp_recid'+inttostr(fm.frameno)+'","v":"'+floattostr(recid)+'","r":"'+'0'+'","t":"s"},';
  end else begin
    rcount:=dbcall.storedata.GetRowCount(fm.FrameNo);
    for I := 1 to rcount do begin
      recid:=dbcall.StoreData.GetParentDocId(fm.FrameNo,i);
      fm.jsonstr:=fm.jsonstr + '{"n":"'+'axp_recid'+inttostr(fm.frameno)+'","v":"'+floattostr(recid)+'","r":"'+inttostr(i)+'","t":"s"},';
    end;
  end;
end;

procedure TASBDataObj.EndDataJSON;
var i:integer;
begin
  jsonstr:='';
  for i:=0 to dbcall.struct.frames.Count - 1 do
  begin
    if pfrm(dbcall.struct.frames[i]).jsonstr <> '' then
       jsonstr:=jsonstr + pfrm(dbcall.struct.frames[i]).jsonstr
    else
       jsonstr:=jsonstr + DummyDynamicDCNodeToJSON(pfrm(dbcall.struct.frames[i]),1,'i1');
  end;

  if jsonstr='' then exit;
  if dbm.gf.MobileWSFlag then
  begin
    act.fldnode := '{"result":[';
    jsonstr := act.fldnode + jsonstr;
    //jsonstr := '{"result":[' + jsonstr;
    act.fldnode := jsonstr;
    if act.fldnode = '{"result":[' then act.fldnode := '{"result":[]}'
    else begin
      delete(act.fldnode,length(act.fldnode),1);
      act.fldnode := act.fldnode + ']}';
    end;
  end
  else
  begin
    jsonstr := act.fldnode + jsonstr;
    act.fldnode := jsonstr;
    if act.fldnode = '{"data":[' then act.fldnode := '{"data":[]}'
    else begin
      delete(act.fldnode,length(act.fldnode),1);
      act.fldnode := act.fldnode + ']}';
    end;
  end;
  jsonstr := act.fldnode ;
end;

procedure TASBDataObj.DCToJSON(fm: pFrm ; rowcount:integer);
  var i,ValidRowPos,dcnumber : integer;
      s,n,dccaption : String;
begin
  s := '';
  dcnumber := fm.FrameNo;
  n:='DC'+inttostr(dcnumber);
  dbm.gf.dodebug.msg('DC to JSON '+ n);
  dccaption := GetDCCaption(fm.caption);
  if (fm.PopIndex > -1) and (not fm.HasDataRows)then exit;
  if rowcount = -1 then
     rowcount := dbcall.StoreData.GetRowCount(dcnumber);
  if rowcount > 0 then
  begin
    if assigned(dbcall) then
    begin
      for i := 0 to dbcall.StoreData.ins_del_rows.Count - 1 do
      begin
         if dbcall.StoreData.ins_del_rows.Names[i] = n then
         begin
            s := dbcall.StoreData.ins_del_rows.Values[dbcall.StoreData.ins_del_rows.Names[i]];
            if copy(s,length(s),1) = ',' then delete(s,length(s),1);
            break;
         end;
      end;
    end;
  end;
  fm.jsonstr := fm.jsonstr + '{';
  if activerow > -1 then
  begin
    if inttostr(dcnumber) = copy(act_fgname,1,pos(',',act_fgname)-1) then
    begin
      if dccaption ='' then
      begin
         if fg_record_count > -1 then
            fm.jsonstr := fm.jsonstr + '"n":"'+n+'","v":"'+inttostr(fg_record_count)+'"'
         else fm.jsonstr := fm.jsonstr + '"n":"'+n+'","v":"'+inttostr(rowcount)+'"'
      end else
      begin
         if fg_record_count > -1 then
            fm.jsonstr := fm.jsonstr + '"n":"'+n+'","c":"'+dccaption+'","v":"'+inttostr(fg_record_count)+'"'
         else fm.jsonstr := fm.jsonstr + '"n":"'+n+'","c":"'+dccaption+'","v":"'+inttostr(rowcount)+'"';
      end;
    end else
    begin
      if dccaption ='' then
         fm.jsonstr := fm.jsonstr + '"n":"'+n+'","v":"'+inttostr(rowcount)+'"'
      else
         fm.jsonstr := fm.jsonstr + '"n":"'+n+'","c":"'+dccaption+'","v":"'+inttostr(rowcount)+'"';
    end;
  end else
  begin
    if dccaption ='' then
       fm.jsonstr := fm.jsonstr + '"n":"'+n+'","v":"'+inttostr(rowcount)+'"'
    else
       fm.jsonstr := fm.jsonstr + '"n":"'+n+'","c":"'+dccaption+'","v":"'+inttostr(rowcount)+'"';
  end;
  if s <> '' then fm.jsonstr := fm.jsonstr + ',"cr":"'+s+'"';
  fm.jsonstr := fm.jsonstr + ',"t":"dc"';
  if fm.AsGrid then
  begin
    if not fm.HasDataRows then
    begin
      ValidRowPos := dbcall.StoreData.GetFieldIndex('validrow'+inttostr(fm.FrameNo), 1);
      if ((validrowpos >= 0) and (uppercase(pFieldRec(dbcall.StoreData.FieldList[validrowpos]).Value)='T')) then
         fm.HasDataRows := true;
    end;
    if fm.HasDataRows then fm.jsonstr := fm.jsonstr + ',"hasdatarows":"yes"'
    else fm.jsonstr := fm.jsonstr + ',"hasdatarows":"no"';
  end;
  if act_fgname <> '' then
  begin
    if inttostr(dcnumber) = copy(act_fgname,1,pos(',',act_fgname)-1) then
    begin
       delete(act_fgname,1,pos(',',act_fgname));
       fm.jsonstr := fm.jsonstr + ',"fg":"'+act_fgname+'"';
    end;
  end;
  fm.jsonstr := fm.jsonstr + '},';
  GetAxpRecId(fm);
end;

procedure TASBDataObj.DCToJSON(dcnumber, rowcount:integer);
  var i,ValidRowPos : integer;
      s,n,dccaption : String;
      fm:pFrm;
begin
  s := '';
  n:='DC'+inttostr(dcnumber);
  dbm.gf.dodebug.msg('DC to JSON '+ n);
  fm:=dbcall.struct.getframe(dcnumber);
  dccaption := GetDCCaption(fm.caption);
  if (fm.PopIndex > -1) and (not fm.HasDataRows)then exit;
  if rowcount = -1 then
     rowcount := dbcall.StoreData.GetRowCount(dcnumber);
  if rowcount > 0 then
  begin
    if assigned(dbcall) then
    begin
      for i := 0 to dbcall.StoreData.ins_del_rows.Count - 1 do
      begin
         if dbcall.StoreData.ins_del_rows.Names[i] = n then
         begin
            s := dbcall.StoreData.ins_del_rows.Values[dbcall.StoreData.ins_del_rows.Names[i]];
            if copy(s,length(s),1) = ',' then delete(s,length(s),1);
            break;
         end;
      end;
    end;
  end;
  fm.jsonstr := fm.jsonstr + '{';
  if activerow > -1 then
  begin
    if inttostr(dcnumber) = copy(act_fgname,1,pos(',',act_fgname)-1) then
    begin
      if dccaption ='' then
      begin
         if fg_record_count > -1 then
            fm.jsonstr := fm.jsonstr + '"n":"'+n+'","v":"'+inttostr(fg_record_count)+'"'
         else fm.jsonstr := fm.jsonstr + '"n":"'+n+'","v":"'+inttostr(rowcount)+'"'
      end else
      begin
         if fg_record_count > -1 then
            fm.jsonstr := fm.jsonstr + '"n":"'+n+'","c":"'+dccaption+'","v":"'+inttostr(fg_record_count)+'"'
         else fm.jsonstr := fm.jsonstr + '"n":"'+n+'","c":"'+dccaption+'","v":"'+inttostr(rowcount)+'"';
      end;
    end else
    begin
      if dccaption ='' then
         fm.jsonstr := fm.jsonstr + '"n":"'+n+'","v":"'+inttostr(rowcount)+'"'
      else
         fm.jsonstr := fm.jsonstr + '"n":"'+n+'","c":"'+dccaption+'","v":"'+inttostr(rowcount)+'"';
    end;
  end else
  begin
    if dccaption ='' then
       fm.jsonstr := fm.jsonstr + '"n":"'+n+'","v":"'+inttostr(rowcount)+'"'
    else
       fm.jsonstr := fm.jsonstr + '"n":"'+n+'","c":"'+dccaption+'","v":"'+inttostr(rowcount)+'"';
  end;
  if s <> '' then fm.jsonstr := fm.jsonstr + ',"cr":"'+s+'"';
  fm.jsonstr := fm.jsonstr + ',"t":"dc"';
  if fm.AsGrid then
  begin
    if not fm.HasDataRows then
    begin
      ValidRowPos := dbcall.StoreData.GetFieldIndex('validrow'+inttostr(fm.FrameNo), 1);
      if ((validrowpos >= 0) and (uppercase(pFieldRec(dbcall.StoreData.FieldList[validrowpos]).Value)='T')) then
         fm.HasDataRows := true;
    end;
    if fm.HasDataRows then fm.jsonstr := fm.jsonstr + ',"hasdatarows":"yes"'
    else fm.jsonstr := fm.jsonstr + ',"hasdatarows":"no"';
  end;
  if act_fgname <> '' then
  begin
    if inttostr(dcnumber) = copy(act_fgname,1,pos(',',act_fgname)-1) then
    begin
       delete(act_fgname,1,pos(',',act_fgname));
       fm.jsonstr := fm.jsonstr + ',"fg":"'+act_fgname+'"';
    end;
  end;
  fm.jsonstr := fm.jsonstr + '},';
  GetAxpRecId(fm);
end;

procedure TASBDataObj.DCToJSON(dcnumber, rowcount , ActRow :integer);
  var i,ValidRowPos : integer;
      s,n,dccaption : string;
      fm:pFrm;
begin
  s := '';
  n:='DC'+inttostr(dcnumber);
  dbm.gf.dodebug.msg('DC to JSON '+ n);
  fm:=dbcall.struct.getframe(dcnumber);
  dccaption := GetDcCaption(fm.caption);
  if (fm.PopIndex > -1) and (not fm.HasDataRows)then exit;
  if rowcount = -1 then
     rowcount := dbcall.StoreData.RowCount(dcnumber);
  if rowcount > 0 then
  begin
    if assigned(dbcall) then
    begin
      for i := 0 to dbcall.StoreData.ins_del_rows.Count - 1 do
      begin
         if dbcall.StoreData.ins_del_rows.Names[i] = n then
         begin
            s := dbcall.StoreData.ins_del_rows.Values[dbcall.StoreData.ins_del_rows.Names[i]];
            if copy(s,length(s),1) = ',' then delete(s,length(s),1);
            break;
         end;
      end;
    end;
  end;
  fm.jsonstr := fm.jsonstr + '{';
  if dccaption='' then
      fm.jsonstr := fm.jsonstr + '"n":"'+n+'","v":"'+inttostr(rowcount)+'"'
  else
     fm.jsonstr := fm.jsonstr + '"n":"'+n+'","c":"'+dccaption+'","v":"'+inttostr(rowcount)+'"';
  if s <> '' then fm.jsonstr := fm.jsonstr + ',"cr":"'+s+'"';
  fm.jsonstr := fm.jsonstr + ',"t":"dc"';
  if fm.AsGrid then
  begin
    if not fm.HasDataRows then
    begin
      ValidRowPos := dbcall.StoreData.GetFieldIndex('validrow'+inttostr(fm.FrameNo), 1);
      if ((validrowpos >= 0) and (uppercase(pFieldRec(dbcall.StoreData.FieldList[validrowpos]).Value)='T')) then
         fm.HasDataRows := true;
    end;
    if fm.HasDataRows then fm.jsonstr := fm.jsonstr + ',"hasdatarows":"yes"'
    else fm.jsonstr := fm.jsonstr + ',"hasdatarows":"no"';
  end;
  fm.jsonstr := fm.jsonstr + '},';
  GetAxpRecId(dcnumber,ActRow);
end;

procedure TASBDataObj.DummyDCNodeToJSON(dcnumber, rowcount:integer;s:string);
  var i,ValidRowPos : integer;
      n,dccaption : String;
      fm:pFrm;
begin
  n:='DC'+inttostr(dcnumber);
  dbm.gf.dodebug.msg('DC to JSON '+ n);
  fm:=dbcall.struct.getframe(dcnumber);
  if fm.jsonstr <> '' then exit;
  dccaption := GetDCCaption(fm.caption);
  fm.jsonstr := fm.jsonstr + '{';
  if dccaption ='' then
     fm.jsonstr := fm.jsonstr + '"n":"'+n+'","v":"'+inttostr(rowcount)+'"'
  else
     fm.jsonstr := fm.jsonstr + '"n":"'+n+'","c":"'+dccaption+'","v":"'+inttostr(rowcount)+'"';
  if s <> '' then fm.jsonstr := fm.jsonstr + ',"cr":"'+s+'"';
  fm.jsonstr := fm.jsonstr + ',"t":"dc"';
  fm.jsonstr := fm.jsonstr + ',"hasdatarows":"no"';
  fm.jsonstr := fm.jsonstr + '},';
end;

procedure TASBDataObj.FieldToJSON(fd:pfld; frec:pFieldRec;rno:integer=-1);
var i:integer;
    n, v, dtype, src, dpm, cv, mr, rowstring :String;
    q : TXDS;
    fm : pFrm;
begin
  if not assigned(frec) then  exit;
  fm:=dbcall.struct.getframe(fd.frameno);
  if (fm.PopIndex > -1) and (not fm.HasDataRows)then exit;
  if fm.jsonstr = '' then
  begin
    DCToJSON(fd.FrameNo,rno);
  end;
  dbm.gf.dodebug.msg('Field to JSON '+frec.fieldname+' row '+inttostr(frec.RowNo));
  q:=fd.QSelect;
  n:=fd.FieldName;
  v:=frec.value;
  if fd.asgrid then rowstring:=inttostr(frec.rowno-existing_gridrows) else rowstring:='0';
  fm.jsonstr := fm.jsonstr + '{';

  if pos(#$D#$A,v) > 0  then v := dbm.gf.FindAndReplace(v,#$D#$A,'<br>');
  if pos(#$A,v) > 0  then v := dbm.gf.FindAndReplace(v,#$A,'<br>');
  if pos(#$D,v) > 0  then v := dbm.gf.FindAndReplace(v,#$D,'<br>');
  v:=dbm.gf.FindAndReplace(v, '"', '^^dq');
  if frec.DataType = 'n' then
  begin
    DbCall.Validate.fld := fd;
    v := DbCall.Validate.FormatNumber(v);
  end;
  if fd.DataType = 't' then
     dtype := 'm'
  else  if fd.DataType = 'i' then
     dtype := 'i'
  else if (fd.ModeofEntry = 'select') and (fd.txtSelection <> true) then
     dtype := 'c'
  else dtype := 's';
  fm.jsonstr := fm.jsonstr + '"n":"'+n+'","v":"'+v+'"';
  fm.jsonstr := fm.jsonstr + ',"r":"'+rowstring+'"';
  cv:='';
  if assigned(q) then begin
    if (not dbm.gf.CopyTrans and fd.HasGridParents) or (not fd.AsGrid) then mr:='0' else mr:='1';
    if fd.FromList then mr:='1';
    if (fd.ModeofEntry = 'select') then
    begin
      if fd.SourceKey then fm.jsonstr := fm.jsonstr + ',"idcol":"yes"' + ',"id":"'+ floattostr(frec.IdValue) +'"' + ',"oldid":"'+ floattostr(frec.OldIdValue)+'"'
      else fm.jsonstr := fm.jsonstr + ',"idcol":"no"'+ ',"id":"'+ floattostr(frec.IdValue) +'"' + ',"oldid":"'+ floattostr(frec.OldIdValue)+'"' ;
      if fd.AsGrid then fm.jsonstr := fm.jsonstr + ',"mr":"'+mr+'"';
    end;
    src := '';
    if (fd.ModeofEntry = 'select') and (fd.txtSelection <> true) then
    begin
      if (not DbCall.struct.quickload) then
      begin
        if not DbCall.struct.QuickDataFlag then
        begin
          dpm:='';
          if assigned(fd.Dependents) then begin
            for i := 0 to fd.dependents.count-1 do begin
              if fd.dependenttypes[i+1]='f' then begin
                dpm:=dpm + fd.dependents[i]+',';
              end;
            end;
          end;
          if dpm<>'' then delete(dpm, length(dpm), 1);
          if (src = '') and ((mr = '0') or ((frec.rowno=1) and (mr='1'))) then cv := DropDownToJSON(fd, dpm);
          if dpm<>'' then
            fm.jsonstr := fm.jsonstr + ',"dpm":"'+dpm+'"';
        end;
      end;
      if fd.ComponentType <>'' then dtype:=fd.ComponentType;
    end;
  end ;
  fm.jsonstr := fm.jsonstr + ',"t":"'+dtype+'"';
  if cv = '' then
     fm.jsonstr := fm.jsonstr + '},'
  else begin
    fm.jsonstr := fm.jsonstr +'},' + cv  ;
  end;
end;

function TASBDataObj.DropDownToJSON(fd:pfld; var dpm:String) : String;
var i,j,p:integer;
    s,s1,dp,dpv,v,tmpdp,tmpval, ChangedDPM: String;
    q:TXDS;
    dfd:pFld;
    bm:TBookmark;
begin
  result := '';
  q:=fd.QSelect;
  if (not assigned(q)) or (not q.CDS.Active) or (fd.FromList) then exit;
  dbm.gf.dodebug.msg('Drop down values to JSON of ' + fd.fieldname + ', count ' + inttostr(q.CDS.RecordCount));
  if q.CDS.RecordCount = 0 then exit;
  bm:=q.CDS.GetBookmark;
  q.CDS.First;
  ChangedDPM:='';
  while not q.CDS.Eof do
  begin
    s := '{';
    v := '';
    if fd.SourceKey then
    begin
      v := q.CDS.Fields[0].text;
      s1 := q.CDS.Fields[1].text;
      if pos(#$A,s1) > 0  then s1 := dbm.gf.FindAndReplace(s1,#$A,'<br>');
      if pos(#$D,s1) > 0  then s1 := dbm.gf.FindAndReplace(s1,#$D,'<br>');
      s1:=dbm.gf.FindAndReplace(s1, '"', '^^dq');
      v := v + '~' + s1;
      s := s + '"v":"'+v+'","t":"dv",';
    end else
    begin
      s1 := q.CDS.Fields[0].text;
      if pos(#$A,s1) > 0  then s1 := dbm.gf.FindAndReplace(s1,#$A,'<br>');
      if pos(#$D,s1) > 0  then s1 := dbm.gf.FindAndReplace(s1,#$D,'<br>');
      s1:=dbm.gf.FindAndReplace(s1, '"', '^^dq');
      v := s1;
      s := s + '"v":"'+v+'","t":"dv",';
    end;
    j := 1; dpv:='';
    while true do
    begin
      dp := dbm.gf.GetnthString(dpm,j);
      if dp = '' then break;
      inc(j);
      s1 := '';
      dfd:=dbcall.struct.getfield(dp);
      if (assigned(dfd)) and (assigned(q.cds.findfield(dfd.SourceField))) then begin
        s1:=q.cds.fieldbyname(dfd.sourcefield).AsString;
        if s1 <> '' then
        begin
          if pos(#$A,s1) > 0  then s1 := dbm.gf.FindAndReplace(s1,#$A,'<br>');
          if pos(#$D,s1) > 0  then s1 := dbm.gf.FindAndReplace(s1,#$D,'<br>');
          s1:=dbm.gf.FindAndReplace(s1, '"', '^^dq');
          if dbm.gf.MobileWSFlag then
          begin
            s1 := dbm.gf.FindAndReplace(s1, #009, #032);
            s1 := dbm.gf.FindAndReplace(s1, '\', '\\');
          end;
        end;
        dpv := dpv + s1 + '~' ;
        if changeddpm<>'*' then ChangedDPM:=ChangedDPM+dp+',';
      end;
    end;
    if (ChangedDPM <> '') and (changeddpm<>'*') then begin
      delete(changeddpm,length(changeddpm),1);
      dpm:=ChangedDPM;
      changeddpm:='*';
    end;
    if dpv <> '' then
    begin
      delete(dpv,length(dpv),1);
      dpv := '"dp":"'+dpv+'"';
      result := result + s + dpv + '},';
    end else begin
      delete(s,length(s),1);
      result := result + s + '},';
    end;
    q.CDS.next;
  end;
  q.CDS.GotoBookmark(bm);
end;

function TASBDataObj.GridToJSON(FrameNo:integer; WithDropDown : boolean; OnlyChanged:Boolean=false ;actrow:integer=-1):integer;
var i,j, k, FirstSavedField, rcount, r:integer;
    fm:pFrm;
    fd:pFld;
    frec:pFieldRec;
begin
  dbm.gf.dodebug.msg('Grid To JSON '+Inttostr(FrameNo));
  fm:=dbcall.Struct.GetFrame(FrameNo);
  rcount:=dbcall.StoreData.RowCount(fm.FrameNo);
  if rcount=0 then rcount:=1;
  if actrow = -1 then
  begin
    for r := 1 to rcount do begin
      if (not DbCall.struct.quickload) then
         if not DbCall.struct.QuickDataFlag then dbcall.validate.RegRow(FrameNo, r);
      for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
        fd:=pFld(dbcall.struct.flds[j]);
        frec:=dbcall.storedata.GetFieldRec(fd.FieldName, r);
        if (onlyChanged) and assigned(frec) and (not frec.ClientNotify) then continue;
        if (fd.ModeofEntry='select') and (not fd.txtSelection) and (WithDropDown) then
            frec:=Dbcall.Validate.LoadDropDown(fd, frec);
        if not assigned(frec) then continue;
        FieldToJSON(fd, frec);
      end;
    end;
  end else
  begin
   for r := actrow to rcount do begin
    for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
      fd:=pFld(dbcall.struct.flds[j]);
      frec:=dbcall.storedata.GetFieldRec(fd.FieldName, r);
      if not assigned(frec) then continue;
      if (onlyChanged) and  (not frec.ClientNotify) then continue;
      FieldToJSON(fd, frec);
    end;
   end;
  end;
  result:=fm.StartIndex+fm.FieldCount;
  if IsImpFromCSVToDC then
  begin
    DbCall.RefreshGridDependents(fm);
    GridDependentsToJSON(dbcall.GridDependentFields);
  end;
  if rcount=1 then ChangeHasDataRowStatus(fm);
end;

function TASBDataObj.LoadDCToJSON(FrameNo:integer):integer;
var i,j, k, FirstSavedField, rcount, r:integer;
    fm:pFrm;
    fd:pFld;
    frec:pFieldRec;
begin
  dbm.gf.dodebug.msg('Load DC To JSON '+Inttostr(FrameNo));
  fm:=dbcall.Struct.GetFrame(FrameNo);
//  GetAxpRecid(FrameNo);
  rcount:=dbcall.StoreData.RowCount(fm.FrameNo);
  if rcount=0 then
  begin
     rcount:=1;
     fm.HasDataRows := false;
  end;
  if  not fm.HasDataRows then
  begin
    for r := 1 to rcount do begin
      dbcall.validate.RegRow(FrameNo, r);
      for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
        fd:=pFld(dbcall.struct.flds[j]);
        frec:=Dbcall.validate.PrepareField(fd);
        if not assigned(frec) then continue;
        FieldToJSON(fd, frec);
      end;
    end;
  end else
  begin
    for r := 1 to rcount do begin
      dbcall.validate.RegRow(FrameNo, r);
      for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
        fd:=pFld(dbcall.struct.flds[j]);
        frec:=dbcall.storedata.GetFieldRec(fd.FieldName, r);
        if (fd.ModeofEntry='select') and (not fd.txtSelection) then begin
          frec:=Dbcall.Validate.LoadDropDown(fd, frec);
          FieldToJSON(fd, frec);
        end else if fd.Exprn>-1 then begin
          if (fd.txtSelection) and (assigned(frec)) and (frec.Value<>'') then continue;
          if (fd.FromList) and (assigned(frec)) and (frec.Value<>'') then continue;
          if (fd.ModeofEntry='accept') and (assigned(frec)) and (frec.Value<>'') then continue;
          frec:=DbCall.Validate.EvalExpression(fd, frec);
          FieldToJSON(fd, frec);
        end;
      end;
    end;
  end;
  result:=fm.StartIndex+fm.FieldCount;
  if DbCall.Validate.FldDependentFields <> '' then GridDependentsToJSON(DbCall.Validate.FldDependentFields);
end;


function TASBDataObj.LoadDCToJSONNew(FrameNo:integer):integer;
var i,j, k, FirstSavedField, rcount, r:integer;
    fm:pFrm;
    fd:pFld;
    frec:pFieldRec;
begin
  dbm.gf.dodebug.msg('Quick Load DC To JSON '+Inttostr(FrameNo));
  fm:=dbcall.Struct.GetFrame(FrameNo);
  rcount:=dbcall.StoreData.RowCount(fm.FrameNo);
  if rcount=0 then
  begin
     rcount:=1;
     fm.HasDataRows := false;
  end;
  if  not fm.HasDataRows then
  begin
    for r := 1 to rcount do begin
      dbcall.validate.RegRow(FrameNo, r);
      for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
        fd:=pFld(dbcall.struct.flds[j]);
        if fd.FrameNo <> fm.FrameNo  then  continue;
        FieldToJSONNew(fd, Dbcall.validate.Prepare_Selected_Field(fd,VisibleDCs,r));
      end;
    end;
  end else
  begin
    for r := 1 to rcount do begin
      dbcall.validate.RegRow(FrameNo, r);
      for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
        fd:=pFld(dbcall.struct.flds[j]);
        if fd.FrameNo <> fm.FrameNo  then  continue;
        frec:=dbcall.storedata.GetFieldRec(fd.FieldName, r);
        if fd.Exprn>-1 then begin
          if (fd.txtSelection) and (assigned(frec)) and (frec.Value<>'') then continue;
          if (fd.FromList) and (assigned(frec)) and (frec.Value<>'') then continue;
          if (fd.ModeofEntry='accept') and (assigned(frec)) and (frec.Value<>'') then continue;
          frec:=DbCall.Validate.EvalExpression(fd, frec);
          FieldToJSON(fd, frec);
        end;
      end;
    end;
  end;
  result:=fm.StartIndex+fm.FieldCount;
  if rcount=1 then ChangeHasDataRowStatus(fm);
  if DbCall.Validate.FldDependentFields <> '' then GridDependentsToJSON(DbCall.Validate.FldDependentFields);
end;


function TASBDataObj.LoadSubGridToJSON(FrameNo:integer):integer;
var j :integer;
    fm:pFrm;
    fd:pFld;
    frec:pFieldRec;
begin
  dbm.gf.dodebug.msg('Load SubGrid To JSON '+Inttostr(FrameNo));
  PopDCToJSON(FrameNo,1);
  GetAxpRecid(FrameNo,1);
  fm:=dbcall.Struct.GetFrame(FrameNo);
  fm.HasDataRows := true;
  for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
    fd:=pFld(dbcall.struct.flds[j]);
    frec:=Dbcall.validate.PrepareField(fd);
    if not assigned(frec) then continue;
    FieldToJSON(fd, frec);
  end;
  result:=fm.StartIndex+fm.FieldCount;
end;

procedure TASBDataObj.PopDCToJSON(dcnumber, rowcount:integer);
  var i,ValidRowPos : integer;
      s,n,s1,dccaption : String;
      fm:pFrm;
begin
  s := '';s1:='';
  n:='DC'+inttostr(dcnumber);
  dbm.gf.dodebug.msg('PopDC to JSON '+ n);
  fm:=dbcall.Struct.GetFrame(dcnumber);
  dccaption := Getdccaption(fm.caption);
  if rowcount = -1 then
     rowcount := dbcall.StoreData.RowCount(dcnumber);
  if assigned(dbcall) then
  begin
    for i := 0 to dbcall.StoreData.ins_del_rows.Count - 1 do
    begin
       if dbcall.StoreData.ins_del_rows.Names[i] = n then
       begin
          s := dbcall.StoreData.ins_del_rows.Values[dbcall.StoreData.ins_del_rows.Names[i]];
          if copy(s,length(s),1) = ',' then delete(s,length(s),1);
         break;
       end;
    end;
    for i := 0 to dbcall.StoreData.subgrid_del_recid_list.Count - 1 do
    begin
       if dbcall.StoreData.subgrid_del_recid_list.Names[i] = n then
       begin
          s1 := dbcall.StoreData.subgrid_del_recid_list.Values[dbcall.StoreData.subgrid_del_recid_list.Names[i]];
          if copy(s1,length(s1),1) = ',' then delete(s1,length(s1),1);
         break;
       end;
    end;
  end;
  if rowcount = 0 then fm.HasDataRows := false;
  fm.jsonstr := fm.jsonstr + '{';
  if dccaption ='' then
     fm.jsonstr := fm.jsonstr + '"n":"'+n+'","v":"'+inttostr(rowcount)+'"'
  else
    fm.jsonstr := fm.jsonstr + '"n":"'+n+'","c":"'+dccaption+'","v":"'+inttostr(rowcount)+'"';
  if s <> '' then fm.jsonstr := fm.jsonstr + ',"cr":"'+s+'"';
  if s1 <> '' then fm.jsonstr := fm.jsonstr + ',"dr":"'+s1+'"';
  fm.jsonstr := fm.jsonstr + ',"t":"dc"';
  if fm.AsGrid then
  begin
    if not fm.HasDataRows then
    begin
      ValidRowPos := dbcall.StoreData.GetFieldIndex('validrow'+inttostr(fm.FrameNo), 1);
      if ((validrowpos >= 0) and (uppercase(pFieldRec(dbcall.StoreData.FieldList[validrowpos]).Value)='T')) then
         fm.HasDataRows := true;
    end;
     if fm.HasDataRows then fm.jsonstr := fm.jsonstr + ',"hasdatarows":"yes"'
     else fm.jsonstr := fm.jsonstr + ',"hasdatarows":"no"';
  end;
  fm.jsonstr := fm.jsonstr + '},';
end;

function TASBDataObj.PopGridToJSON(popGrid:pPopGrid; RowNo:Integer; WithDropDown:Boolean):integer;
var i,j,k, r:integer;
    fm:pFrm;
    fd:pFld;
    frec:pFieldRec;
    s:String;
    ActualRows, ParentList:TStringList;
    axprecid_added : boolean;
begin
  dbm.gf.DoDebug.msg('Popgrid to JSON '+IntToStr(PopGrid.FrameNo)+' Parent row '+IntToStr(RowNo));
  ActualRows:=TStringList.create;
  ParentList:=TStringList.create;
  dbcall.validate.RegRow(PopGrid.ParentFrameNo, rowno);
  dbcall.Validate.GetParentValue(popgrid.FrameNo,RowNo,popgrid.ParentField,ParentList);
  if popgrid.AutoFillFld then
    ParentList.Add('sub'+Trim(inttostr(popgrid.FrameNo))+'_autofill=T');
  ActualRows.CommaText := dbcall.validate.GetActualRows(popgrid.FrameNo,ParentList);

  fm:=dbcall.Struct.GetFrame(popGrid.FrameNo);
  if (fm.HasDataRows) or (ActualRows.count>0) then
  begin
    for r := 0 to ActualRows.count-1 do begin
      RowNo:=StrToInt(ActualRows[r]);
      dbcall.validate.GetParentActiveRow(rowno,pfrm(dbcall.struct.frames[popgrid.FrameNo-1]).popindex);
      dbcall.validate.RegRow(fm.FrameNo, rowno);
//      GetAxpRecid(PopGrid.FrameNo);
      axprecid_added := false;
      for I := fm.StartIndex to fm.StartIndex+fm.FieldCount-1 do begin
        fd:=pFld(dbcall.Struct.flds[i]);
        for j := 0 to fd.DataRows.count - 1 do begin
          k:=strtoint(fd.DataRows[j]);
          frec:=pFieldRec(dbcall.StoreData.Fieldlist[k]);
          if frec.rowno <> RowNo  then continue;
          if (fd.ModeofEntry='select') and (not fd.txtSelection) and (WithDropDown) then
            Dbcall.Validate.LoadDropDown(fd, frec);
          if not axprecid_added then
          begin
             PopGrrid_GetAxpRecid(PopGrid.FrameNo,rowno,frec.RecordId);
             axprecid_added := true;
          end;
          FieldToJSON(fd, frec);
        end;
      end;
    end;
  end;
  ActualRows.Clear;
  ActualRows.Free;
  ParentList.Clear;
  ParentList.free;
  result:=fm.StartIndex+fm.FieldCount;
end;

procedure TASBDataObj.PopGrrid_GetAxpRecid(frameno, rowno:integer;recid :extended );
begin
  pFrm(dbcall.struct.frames[frameno-1]).jsonstr := pFrm(dbcall.struct.frames[frameno-1]).jsonstr + '{"n":"'+'axp_recid'+inttostr(frameno)+'","v":"'+floattostr(recid)+'","r":"'+inttostr(rowno)+'","t":"s"},';
end;

Procedure TASBDataObj.GridDependentsToJSON(GridDependents:String);
var i,k,r:integer;
  s:String;
  fld:pFld;
  fm:pFrm;
  frec : PFieldRec;
begin
  if DbCall.struct.quickload then
  begin
    i := 1;
    while true do
    begin
      s := axprovider.dbm.gf.GetnthString(GridDependents,i);
      if s = '' then break;
      fld:=dbcall.struct.GetField(s);
      if fld.AsGrid then
      begin
         k := dbcall.StoreData.RowCount(fld.FrameNo);
         fm:=dbcall.Struct.GetFrame(fld.FrameNo);
         for r := 1 to k do
         begin
           if fm.Popup then
              dbcall.Validate.GetParentActiveRow(r,fm.popindex)
           else
              dbcall.Validate.Parser.Registervar('activeprow', Char('n'), inttostr(r));
           dbcall.Validate.RegRow(fld.FrameNo , r);
           FieldToJSONNew(fld, DbCall.Validate.RefreshField(fld, r,true));
         end;
      end
         else FieldToJSONNew(fld, DbCall.Validate.RefreshField(fld, 1,true));
      inc(i);
    end;
  end else
  begin
    GridDependents := ',' + GridDependents + ',';
    for i:=0 to dbcall.struct.flds.count-1 do begin
      fld:=pfld(dbcall.struct.flds[i]);
      if pos(','+fld.FieldName+',', GridDependents) > 0 then
      begin
        dbcall.validate.RefreshField(fld, 1,true);
        frec:=dbcall.StoreData.GetFieldRec(fld.FieldName, 1);
        if assigned(frec) then FieldToJSON(fld, frec);
      end;
    end;
  end;
end;

Procedure TASBDataObj.GridDependentsToJSON(GridDependents:String;fm:pFrm);
var i:integer;
s:String;
fld:pFld;
frec:pFieldRec;
begin
  GridDependents := ',' + GridDependents + ',';
  for i := fm.StartIndex to fm.StartIndex+fm.FieldCount-1 do begin
    fld:=pfld(dbcall.struct.flds[i]);
    if pos(','+fld.FieldName+',', GridDependents) > 0 then
    begin
      dbcall.validate.RefreshField(fld, 1,true);
      frec:=dbcall.StoreData.GetFieldRec(fld.FieldName, 1);
      if assigned(frec) then FieldToJSON(fld, frec);
    end;
  end;
end;


Procedure TASBDataObj.GridDependentsToJSON(GridDependents:String; depflds : TStringList);
var i,k,r:integer;
  s:String;
  fld:pFld;
  fm:pFrm;
  frec : PFieldRec;
begin
  i := 1;
  while true do
  begin
    s := axprovider.dbm.gf.GetnthString(GridDependents,i);
    if s = '' then break;
    if depflds.IndexOf(s) = -1 then
    begin
      fld:=dbcall.struct.GetField(s);
      if fld.AsGrid then
      begin
         k := dbcall.StoreData.RowCount(fld.FrameNo);
         fm:=dbcall.Struct.GetFrame(fld.FrameNo);
         for r := 1 to k do
         begin
           if fm.Popup then
              dbcall.Validate.GetParentActiveRow(r,fm.popindex)
           else
              dbcall.Validate.Parser.Registervar('activeprow', Char('n'), inttostr(r));
           dbcall.Validate.RegRow(fld.FrameNo , r);
           FieldToJSONNew(fld, DbCall.Validate.RefreshField(fld, r,true));
         end;
      end
         else FieldToJSONNew(fld, DbCall.Validate.RefreshField(fld, 1,true));
    end;
    inc(i);
  end;
end;

procedure TASBDataObj.ImageFieldToJSON(fd:pfld;SrcImgDet:String='';frec:pFieldRec=nil;rowstring:string='0');
var i,r:integer;
    n, v, dtype, tname,fval,tid,tfile,sname,v1,fval1,frecval,imgPath,fldval,sourcefld,s:string;
    fm : pFrm;
    f : extended;
    sr: TSearchRec;
    frec_image : pFieldRec;
    tmp_fd : pFld;
begin
  fm:=dbcall.Struct.GetFrame(fd.FrameNo);
  if fm.jsonstr = '' then
  begin
    DCToJSON(fd.FrameNo,-1);
  end;
  dbm.gf.dodebug.msg('Field to JSON '+fd.fieldname);
  n:=fd.FieldName;
  imgPath := Trim(dbcall.Parser.GetVarValue('AxpImagePath'));
  if dbcall.struct.HasImgPath then
  begin
     s := 'dc'+trim(inttostr(fd.FrameNo))+'_imagepath' ;
     dbm.gf.dodebug.msg('Image Path '+ s);
     tmp_fd := dbcall.struct.GetField(s);
     if assigned(tmp_fd) then
     begin
        s := dbcall.StoreData.GetFieldValue(s,1);
        if s = '' then imgPath := '';
     end;
     dbm.gf.dodebug.msg('Image Path Value '+ s);
  end;
  if SrcImgDet = '' then begin
    if trim(dbcall.struct.SchemaName) <> '' then
      tname := dbcall.struct.SchemaName + '.' + dbcall.transid+trim(fd.FieldName)
    else
      tname := dbcall.transid+trim(fd.FieldName);
    f := strtofloat(dbCall.Parser.GetVarValue('recordid'));
    if f = 0 then
    begin
       if pos('getsourceimage',lowercase(fd.cexp)) > 0 then
       begin
         tname := Axprovider.dbm.gf.GetNthString(fd.cexp,1);
         delete(tname,1,pos('{',tname));
         delete(tname,pos('}',tname),1);
         n := Axprovider.dbm.gf.GetNthString(fd.cexp,2);
         delete(n,pos('{',n),1);
         delete(n,pos('}',n),1);
         if trim(dbcall.struct.SchemaName) <> '' then
           tname := dbcall.struct.SchemaName + '.' + tname+n
         else
           tname :=tname+n;
         fval := Axprovider.dbm.gf.GetNthString(fd.cexp,3);
         delete(fval,pos(')',fval),2);
         fval := trim(fval);
         fval := dbCall.Validate.Parser.GetVarValue(fval);
         dbm.gf.dodebug.Msg('Record ID : ' + fval);
         if (fval <> '') and (isnumeric(fval)) then f := StrToFloat(fval);
         fval1 := fval;
      end else if (dbCall.dc_image_fillgrid) and (imgPath = '') then
      begin
          if rowstring = '0' then
             r := 1
          else r := strtoint(rowstring);
          dbm.gf.dodebug.Msg('Getting fillgrid image details');
          frec_image:=dbcall.storedata.GetFieldRec('dc2_image_source_transid', r);
          if assigned(frec_image) then
          begin
             tname := frec_image.Value;
          end;
          frec_image:=dbcall.storedata.GetFieldRec('dc2_image_source_fldname', r);
          if assigned(frec_image) then
          begin
             n := frec_image.Value;
          end;
          if trim(dbcall.struct.SchemaName) <> '' then
            tname := dbcall.struct.SchemaName + '.' + tname+n
          else
            tname :=tname+n;
          frec_image:=dbcall.storedata.GetFieldRec('dc2_image_source_recordid', r);
          if assigned(frec_image) then
          begin
             f := StrToFloat(frec_image.Value) ;
          end;
      end;
    end;
  end else begin
    tname := Axprovider.dbm.gf.GetNthString(SrcImgDet,1);
    fval := Axprovider.dbm.gf.GetNthString(SrcImgDet,2);
    f := StrToFloat(fval);
    fval1 := fval;
  end;
  if assigned(frec) then fldval := frec.Value;

  dbm.gf.dodebug.Msg('Getting Image : ');
  dbm.gf.dodebug.Msg('Table Name : ' + tname);
  dbm.gf.dodebug.Msg('Image Name : ' + n);
  dbm.gf.dodebug.Msg('Record ID : ' + floattostr(f));
  dbm.gf.dodebug.Msg('Field Value : ' + fldval);
  sourcefld := '';
  if ((imgPath = '') and ((fd.FieldName = 'dc' + inttostr(fd.FrameNo) + '_image') or (fd.DataType='i'))) or ((fd.DataType='i') and (imagefromdb)) then
  begin
    v := axprovider.LoadImage(tname,n,fd.DataType,fldval,f);
//    if (pos('getsourceimage',lowercase(fd.cexp)) = 0) then v := '';
  end else begin
    if (pos('getsourceimage',lowercase(fd.cexp)) > 0) and (dbCall.Parser.GetVarValue('recordid')='0') then
    begin
      v := imgPath;
      if copy(v,length(v),1) = '\' then delete(v,length(v),1);
      v1 := v;
      tid := Axprovider.dbm.gf.GetNthString(fd.cexp,1);
      delete(tid,1,pos('{',tid));
      delete(tid,pos('}',tid),1);
      tid := trim(tid);
      if (pos('getsourceimage',tid) > 0) then delete(tid,1,15);
      tid := dbCall.Parser.GetVarValue(tid);
      v := v +'\' + tid;
      v1 := v1 + '\' + tid;
      tid := Axprovider.dbm.gf.GetNthString(fd.cexp,2);
      delete(tid,pos('{',tid),1);
      delete(tid,pos('}',tid),1);
      v := v +'\' + tid +'\' + fval;
      v1 := v1 + '\' + tid;
      {
      tfile := v + '\*.*';
      if (findfirst(tfile, faAnyFile, sr) = 0) then
      begin
        repeat
          if (sr.Name = '.') or (sr.Name = '..') then
            continue;
          sname := sr.Name;
        until FindNext(sr) <> 0;
        SysUtils.FindClose(sr);
      end;
      v := v + '\' + sname;
      }
      tfile := AxProvider.dbm.gf.StartPath + '\' + tid + '\' + fval;
      sourcefld := tid;
      if not DirectoryExists(AxProvider.dbm.gf.StartPath + '\' + tid) then ForceDirectories(AxProvider.dbm.gf.StartPath + '\' + tid);
      dbm.gf.dodebug.Msg('Image Source : ' + v);
      dbm.gf.dodebug.Msg('Image copied to : ' + tfile);
      if FileExists(v) then
      begin
        copyfile(pChar(v),pChar(tFile),False);
        v := sname;
      end else
      begin
        v := v +'.*';
        if (findfirst(v, faAnyFile, sr) = 0) then
        begin
          repeat
            if (sr.Name = '.') or (sr.Name = '..') then
              continue;
            sname := sr.Name;
          until FindNext(sr) <> 0;
          SysUtils.FindClose(sr);
        end;
        v1 := v1 + '\' + sname;
        tfile := AxProvider.dbm.gf.StartPath + '\' + tid + '\' + sname;
        sourcefld := tid;
        if not DirectoryExists(AxProvider.dbm.gf.StartPath + '\' + tid) then ForceDirectories(AxProvider.dbm.gf.StartPath + '\' + tid);
        dbm.gf.dodebug.Msg('Image Source : ' + v1);
        dbm.gf.dodebug.Msg('Image copied to : ' + tfile);
        if FileExists(v1) then
        begin
          copyfile(pChar(v1),pChar(tFile),False);
          v := sname;
        end else v := '';
      end;
    end else
    begin
      dbm.gf.dodebug.Msg('Image handling');
      v1 := '';
      v := imgPath;
      if copy(v,length(v)-1,1) = '\' then delete(v,length(v)-1,1);
      if rowstring = '0' then
         r := 1
      else r := strtoint(rowstring);
      if (dbCall.dc_image_fillgrid) and (imgPath <> '') then
      begin
        dbm.gf.dodebug.Msg('Getting fillgrid image details');
        frec_image:=dbcall.storedata.GetFieldRec('dc2_image_source_transid', r);
        if assigned(frec_image) then
        begin
           v := v + '\' + frec_image.Value;
        end;
        frec_image:=dbcall.storedata.GetFieldRec('dc2_image_source_fldname', r);
        if assigned(frec_image) then
        begin
           v := v + '\' + frec_image.Value;
        end;
        frec_image:=dbcall.storedata.GetFieldRec('dc2_image_source_recordid', r);
        if assigned(frec_image) then
        begin
          if Assigned(frec) then
          begin
             v1 := '\'+ frec_image.Value + '-' +frec.Value ;
             frecval := frec.Value ;
          end;
        end;
      end;
      tfile := v + v1;
      dbm.gf.dodebug.Msg('Image to move : ' + tfile);
      if v1 <> '' then
      begin
        if (findfirst(tfile, faAnyFile, sr) = 0) then
        begin
          repeat
            if (sr.Name = '.') or (sr.Name = '..') then
              continue;
            sname := sr.Name;
            break;
          until FindNext(sr) <> 0;
          SysUtils.FindClose(sr);
        end;
        v := v + '\' + sname;
        if frecval <> '' then tfile := AxProvider.dbm.gf.StartPath + '\' + frecval
        else tfile := AxProvider.dbm.gf.StartPath + '\' + sname;
        dbm.gf.dodebug.Msg('Image Source : ' + v);
        dbm.gf.dodebug.Msg('Image copied to : ' + tfile);
        if FileExists(v) then
        begin
          copyfile(pChar(v),pChar(tFile),False);
          if frecval <> '' then v := frecval
          else v := tfile;
        end else v := '';
      end else
      begin
        dbm.gf.dodebug.Msg('No Attachments');
        v := '';
      end;
    end;
  end;
  v := dbm.gf.findandreplace(v,'\','\\');
  if fd.FieldName = 'dc' + inttostr(fd.FrameNo) + '_image' then
  begin
    if (v = '') and (assigned(frec)) then
       v := frec.Value;
    dtype := 's'
  end else dtype := 'i';
  if (sourcefld <> '') and (v <> '') then v := sourcefld + '\\' + v;
  fm.jsonstr := fm.jsonstr + '{';
  fm.jsonstr := fm.jsonstr + '"n":"'+fd.FieldName+'","v":"'+v+'"';
  fm.jsonstr := fm.jsonstr + ',"r":"'+rowstring+'"';
  fm.jsonstr := fm.jsonstr + ',"t":"'+dtype+'"';
  fm.jsonstr := fm.jsonstr + '},'
end;

function TASBDataObj.GetMustLoadFields : String;
  var i,j : integer;
  fld : pFld;
  loadfldlist,s,totfldlist,loadwithdep : String;
begin
  result := '';
  loadfldlist := ''; totfldlist := '';loadwithdep := '';
  fld:=dbcall.struct.GetField('axp_mustloadfields');
  if assigned(fld) then loadfldlist := fld.cexp;
  fld:=dbcall.struct.GetField('axp_mustloadfieldswithdep');
  if assigned(fld) then
  begin
     loadwithdep := fld.cexp;
     if loadwithdep <> '' then
     begin
       if loadfldlist <> '' then
       begin
         loadfldlist := loadfldlist + ',';
         i := 1;
         while True do
         begin
           s:=dbm.gf.getnthstring(loadfldlist,i);
           if s='' then break;
           inc(i);
           totfldlist := totfldlist + quotedstr(s) +',';
         end;
       end;
       loadwithdep := loadwithdep + ',';
       j := 1;
       while True do
       begin
         s:=dbm.gf.getnthstring(loadwithdep,j);
         if s='' then break;
         inc(j);
         if pos(quotedstr(s),totfldlist) <= 0 then
         begin
            loadfldlist := loadfldlist + s + ',';
            totfldlist := totfldlist + quotedstr(s) +',';
            fld := dbcall.struct.GetField(s);
            if assigned(fld) then
            begin
               if assigned(fld.Dependents) then
               begin
                  for i := 0 to fld.Dependents.count-1 do
                  begin
                     if (fld.DependentTypes[i+1]<>'d')and (fld.DependentTypes[i+1]<>'g') then
                     begin
                       if pos(quotedstr(fld.dependents[i]),totfldlist) <= 0 then
                       begin
                          loadfldlist := loadfldlist + fld.dependents[i] + ',';
                          totfldlist := totfldlist + quotedstr(fld.dependents[i]) +',';
                       end;
                     end;
                  end;
               end;
            end;
         end;
       end;
     end;
  end;
  s := GetMdMapFields(totfldlist);
  loadfldlist := loadfldlist + s;
  result := loadfldlist;
end;

function TASBDataObj.GetMdMapFields(totfldlist : String) : String;
  var x : TXDS;
  s,loadfldlist,sql : String;
  fld : pFld;
begin
  result := ''; loadfldlist := '';
  try
  try
    sql := 'select mfield from axrelations where ' + dbm.gf.sqllower +'(mstruct)= ' + quotedstr(lowercase(dbcall.transid)) + ' and rtype = ''md''';
    x := dbm.GetXDS(x);
    x.buffered := true;
    x.CDS.CommandText := sql;
    x.open;
    while not x.CDS.Eof do begin
      s := x.CDS.Fields[0].AsString;
      if pos(quotedstr(s),totfldlist) <= 0 then
      begin
        loadfldlist := loadfldlist + s + ',';
        totfldlist := totfldlist + quotedstr(s) +',';
      end;
      x.CDS.next;
    end;
    except on e:exception do
     begin
       dbm.gf.DoDebug.msg('SQL Test : ' + sql);
       dbm.gf.DoDebug.msg('Error in Open CDS : ' + e.Message);
     end;
    end;
  finally
    x.close;
    FreeAndNil(x);
  end;
  result := loadfldlist;
end;

procedure TASBDataObj.GetMustLoadFieldValues(mustloadflds,recid : String);
  var i : integer;
  s,wherestr,ptable: String;
  fld : pFld;
  fm : pFrm;
  mustloadCDS : TXDS;
begin
  mustloadCDS := nil;
  i := 1;
  while True do
  begin
    s:=dbm.gf.getnthstring(mustloadflds,i);
    if s='' then break;
    inc(i);
    fld := dbcall.struct.GetField(s);
    if assigned(fld) then
    begin
      if fld.SaveValue then
      begin
         pFrm(dbcall.struct.frames[fld.FrameNo-1]).mustloadsql := pFrm(dbcall.struct.frames[fld.FrameNo-1]).mustloadsql + fld.FieldName + ',';
      end;
    end;
  end;
  for i := 0 to dbcall.struct.frames.Count-1 do
  begin
    fm:=pFrm(dbcall.struct.frames[i]);
    delete(fm.mustloadsql,length(fm.mustloadsql),1);
    if fm.FrameNo = 1 then
    begin
       ptable := fm.TableName;
       if fm.mustloadsql <> '' then
       begin
         wherestr := ' where ' + fm.TableName + 'id=' + recid;
         fm.mustloadsql := 'Select ' + fm.TableName + 'id,' + fm.mustloadsql + ' from ' + fm.TableName +  wherestr;
       end else begin
         fm.jsonstr := fm.jsonstr + '{"n":"'+'axp_recid'+inttostr(fm.frameno)+'","v":"'+recid+'","r":"'+'0'+'","t":"s"},';
       end;
    end {else if fm.PopIndex > -1 then
    begin

    end} else
    begin
       if fm.mustloadsql <> '' then fm.mustloadsql := ',' + fm.mustloadsql;
       wherestr := ' where ' + ptable + 'id=' + recid;
       fm.mustloadsql := 'Select ' + fm.TableName + 'id' + fm.mustloadsql + ' from ' + fm.TableName + wherestr + ' order by ' + fm.TableName + 'row';
    end;
    if fm.mustloadsql <> '' then
    begin
       if not assigned(mustloadCDS) then
       begin
         mustloadCDS := dbm.GetXDS(mustloadCDS);
         mustloadCDS.buffered := true;
       end else mustloadCDS.close;
       mustloadCDS.CDS.CommandText := fm.mustloadsql;
       try
         mustloadCDS.open;
         if mustloadCDS.CDS.RecordCount > 0 then  LoadCDSAndCreateJSON(fm,mustloadCDS);
       except on e:exception do
         begin
           dbm.gf.DoDebug.msg('SQL Test : ' + fm.mustloadsql);
           dbm.gf.DoDebug.msg('Error in Open CDS : ' + e.Message);
         end;
       end;
    end;
  end;
  if assigned(mustloadCDS) then
  begin
     mustloadCDS.close;
     FreeAndNil(mustloadCDS);
  end;
end;

procedure TASBDataObj.LoadCDSAndCreateJSON(fm : pFrm;mustloadCDS : TXDS);
  var i,row : integer;
  n,v : String;
begin
  n:='DC'+inttostr(fm.FrameNo);
  fm.jsonstr := fm.jsonstr + '{';
  fm.jsonstr := fm.jsonstr + '"n":"'+n+'","v":"'+inttostr(mustloadCDS.CDS.RecordCount)+'"';
  fm.jsonstr := fm.jsonstr + ',"t":"dc"';
  fm.jsonstr := fm.jsonstr + '},';
  fm.jsonstr := fm.jsonstr + '{';
  mustloadCDS.CDS.First;
  row := 1;
  while not mustloadCDS.CDS.Eof do begin
    for i:=0 to mustloadCDS.CDS.FieldCount-1 do begin
      n := lowercase(mustloadCDS.CDS.Fields[i].FieldName);
      v := mustloadCDS.CDS.Fields[i].AsString;
      if n = lowercase(fm.TableName) + 'id' then
      begin
         fm.jsonstr := fm.jsonstr + '{"n":"'+'axp_recid'+inttostr(fm.frameno)+'","v":"'+v+'","r":"'+inttostr(row)+'","t":"s"},';
      end else
      begin
        if pos(#$A,v) > 0  then v := dbm.gf.FindAndReplace(v,#$A,'<br>');
        if pos(#$D,v) > 0  then v := dbm.gf.FindAndReplace(v,#$D,'<br>');
        v:=dbm.gf.FindAndReplace(v, '"', '^^dq');
        fm.jsonstr := fm.jsonstr + '"n":"'+n+'","v":"'+v+'"';
        fm.jsonstr := fm.jsonstr + ',"r":"'+inttostr(row)+'"';
        fm.jsonstr := fm.jsonstr + '},'
      end;
    end;
    inc(row);
    mustloadCDS.CDS.next;
  end;
end;

procedure TASBDataObj.RefreshDC(FrmNo:Integer);
var j,r,rcount : integer;
    fd : pfld;
    fm : pFrm;
    frec:pFieldRec;
begin
  dbm.gf.dodebug.msg('Load DC To JSON '+Inttostr(FrmNo));
  dbCall.RefreshFrame(FrmNo);
  fm:=dbcall.Struct.GetFrame(FrmNo);
  rcount:=dbCall.StoreData.RowCount(fm.FrameNo);
  for r := 1 to rcount do begin
    for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
      fd:=pFld(dbCall.struct.flds[j]);
      frec:=dbCall.storedata.GetFieldRec(fd.FieldName, r);
      if not assigned(frec) then continue;
      FieldToJSON(fd, frec);
    end;
  end;
end;

procedure TASBDataObj.ChangeHasDataRowStatus(fm : pFrm);
  var i,ValidRowPos : integer;
  s : string;
begin
    if fm.HasDataRows then exit;
    if not fm.HasDataRows then
    begin
      ValidRowPos := dbcall.StoreData.GetFieldIndex('validrow'+inttostr(fm.FrameNo), 1);
      if ((validrowpos >= 0) and (uppercase(pFieldRec(dbcall.StoreData.FieldList[validrowpos]).Value)='T')) then
         fm.HasDataRows := true;
    end;
    if fm.HasDataRows then
    begin
       i := pos('"hasdatarows"',fm.jsonstr)+20;
       s := copy(fm.jsonstr,1,i);
       delete(fm.jsonstr,1,i);
       s := dbm.gf.FindAndReplace(s,'no','yes');
       fm.jsonstr :=  s + fm.jsonstr;
    end;
end;

function TASBDataObj.LoadAndSave(pTransid, pRecordId:String):String;
var i : integer;
    flag : boolean;
begin
  flag := false;
  if (dbTransid <> pTransid) then begin
    if assigned(dbresave) then begin
      dbresave.destroy;
      dbresave := nil;
    end;
  end;
  if not assigned(dbresave) then begin
    dbresave:=TDbCall.create;
    dbresave.Dbm := axprovider.dbm;
    dbresave.axprovider := axprovider;
  end;
  AxProvider.DeleteFromTransCheck;
  dbresave.transid := pTransid;
  dbTransid := pTransid;
  dbresave.LoadData(strtofloat(pRecordid));
  for i := 0 to dbresave.NewMaxRows.Count - 1 do begin
    if dbresave.NewMaxRows[i] = '*' then continue;
    dbresave.NewMaxRows[i] := inttostr(dbresave.StoreData.GetRowCount(i+1));
  end;
  try
    result:=dbresave.ValidateData;
    if result <> ''then begin
      dbm.gf.dodebug.Msg('LoadAndSave Validate error msg : ' + result);
      dbm.gf.ErrorInActionExecution := result;
      Raise Exception.Create(result);
    end;
    flag := axprovider.dbm.InTransaction;
    if not flag then axprovider.dbm.StartTransaction(axprovider.dbm.gf.connectionname);
    result:=dbresave.SaveData;
    if not flag then axprovider.dbm.Commit(axprovider.dbm.gf.connectionname);
  Except
    On E:Exception do begin
      if not flag then axprovider.dbm.RollBack(axprovider.dbm.gf.connectionname);
      dbm.gf.ErrorInActionExecution := E.Message;
      if assigned(dbresave) then begin
        dbresave.destroy;
        dbresave := nil;
      end;
      Raise Exception.Create(E.Message);
    end;
  end;
  if assigned(dbresave) then begin
    dbresave.destroy;
    dbresave := nil;
  end;
end;

function TASBDataObj.GetDCCaption(dccaption:string):string;
var caption,FieldinCaption,Fieldvalue,dccap:string;i,npos,epos,n:integer;
begin
  if not ChangeDCCap then
  begin
    Result :='';
    Exit;
  end;
  i:=1;
  if (Pos('{',trim(dccaption))=0) and (Pos('}',trim(dccaption))=0) then exit;
  caption := dccaption;
  while (i<=length(Caption)) do
  begin
    nPos := Pos('{',caption);
    ePos := Pos('}',caption);
    n := epos-npos;
    FieldinCaption  := Copy(Caption,nPos+1,n-1);
    Delete(Caption,nPos,length(FieldinCaption)+2);
    Fieldvalue :=dbcall.Parser.GetVarValue(Fieldincaption);
    Dccaption := AxProvider.dbm.gf.Replacestr(dccaption,'{'+Fieldincaption+'}',Fieldvalue);
    Dccaption := AxProvider.dbm.gf.FindAndReplace(dccaption, '"', '^^dq');
    inc(i);
  end;
  Result := dccaption;
end;

function TASBDataObj.DummyDynamicDCNodeToJSON(fm : pFrm ; rowcount: integer;s:string):string;
var i,ValidRowPos : integer;
      n,dccaption : String;
begin
  n:='DC'+inttostr(fm.frameno);
  dbm.gf.dodebug.msg('DC to JSON '+ n);
  dccaption := GetDcCaption(fm.caption);
  if dccaption ='' then
  begin
     Result:= '';
     Exit;
  end;
  dccaption := dbm.gf.findandreplace(dccaption,'\','\\');
  dccaption := dbm.gf.findandreplace(dccaption,'"','^^dq');
  fm.jsonstr := fm.jsonstr + '{';
  fm.jsonstr := fm.jsonstr + '"n":"'+n+'","c":"'+dccaption+'","v":"'+inttostr(rowcount)+'"';
  if s <> '' then fm.jsonstr := fm.jsonstr + ',"cr":"'+s+'"';
  fm.jsonstr := fm.jsonstr + ',"t":"dc"';
  fm.jsonstr := fm.jsonstr + ',"hasdatarows":"no"';
  fm.jsonstr := fm.jsonstr + '},';
  Result := fm.jsonstr;
end;

function TASBDataObj.CheckFileExistence(TransId, DocName : String) : String;
Var
  xQry : TXDS;
  sText : String;
begin
  Result := '';
  Try
    dbm.gf.dodebug.msg('Checking the file existence...');
    DocName := LowerCase(DocName);
    xQry := dbm.GetXDS(nil);
    xQry.buffered := True;
    xQry.Close;
    sText := 'Select Username, ordno, status from AxDSignTrans where STransId = :TransId '+
              ' and '+dbm.gf.SQLLower+'(documentname) = :docname order by status ';
    xQry.CDS.CommandText := sText;
    xQry.AssignParam(0,TransId,'c');
    xQry.AssignParam(1,DocName,'c');
    xQry.Open;
    if xQry.CDS.RecordCount > 0 then
    begin
      //Fetching the pending users
      sText := '';
      while not xQry.CDS.Eof do
      begin
        if xQry.CDS.FieldByName('status').AsInteger <> 1 then
        begin
          xQry.CDS.Next;
          Continue;
        end;
        sText := xQry.CDS.FieldByName('UserName').AsString;
        Break;
      end;
      Result := '{"docname":"'+docname+'","docuser":"'+sText+'"}';
      dbm.gf.dodebug.msg('CheckFileExistence result: '+ Result);
    end;
  Finally
    if Assigned(xQry) then
      FreeAndNil(xQry);
  End;
end;

function TASBDataObj.DeleteExistence(TransId, DocName : String) : Boolean;
Var
  xQry : TXDS;
  sText : String;
begin
  Result := False;
  Try
    dbm.gf.dodebug.msg('Checking the details for delete...');
    DocName := LowerCase(DocName);
    xQry := dbm.GetXDS(nil);
    xQry.buffered := True;
    xQry.Close;
    sText := 'select username from AxDSignTrans where STransId = :transid '+
                ' and '+dbm.gf.SQLLower+'(documentname) = :docname and status = 1';
    xQry.CDS.CommandText := sText;
    xQry.AssignParam(0,TransId,'c');
    xQry.AssignParam(1,DocName,'c');
    xQry.Open;
    sText := GetCurrentDir + '\DigiSign\'+TransId+'\';
    if xQry.CDS.RecordCount > 0 then
      sText := sText + xQry.CDS.FieldByName('UserName').AsString
    else
      sText := sText + 'Final';
    sText := sText + '\'+ DocName;
    Try
      dbm.gf.dodebug.msg('File path: '+ sText);
      if FileExists(sText) then
      begin
        DeleteFile(Pchar(sText));
        dbm.gf.dodebug.msg('File has been deleted.');
      end;
      if Assigned(act) then
        if DSignStatus = 'withdraw' then
          act.DSignMsg := 'Document has been withdrawn successfully.'
        else
          act.DSignMsg := 'Document has been overwritten successfully.';
    Except
      On E:Exception do
        dbm.gf.dodebug.msg('Exception on file deletion : ' + E.Message);
    End;

    sText := 'delete from AxDSignTrans where STransId = '+QuotedStr(TransId)+
                ' and '+dbm.gf.SQLLower+'(documentname) = ' + QuotedStr(DocName);
    AxProvider.ExecSQL(sText,'','',False);
    dbm.gf.dodebug.msg('Deleted the Transaction data');
  Finally
    if Assigned(xQry) then
      FreeAndNil(xQry);
  End;

end;

function TASBDataObj.UpdateDigiSign(TransId, PrintDocName, FileName : String):String;
Label
  StopProc;
Var
  sText, DocName, ServerDate, Status, AxDSignType, DocType, DSignResult : String;
  xQry : TXDS;
  DSignTypeFld : PFld;
  SendMailFlag, SendSMSFlag : Boolean;
  xml : IXMLDocument;
  mailnode : IXMLNode;
  DocumentName, AxpMailId, MailId, MobileNo, Subject, Content : String;
  Function GetValue(FldName : String) : String;
  begin
    Result := FldName;
    If Assigned(act.StoreData) then
    begin
      Result := act.StoreData.GetFieldValue(FldName,1);
      If trim(Result) = '' then
      begin
        if dbm.gf.isservice then
        begin
          if Assigned(act.CoreParser) then
            Result := act.CoreParser.GetVarValue(FldName);
        end;
      end
      else if dbm.gf.isservice then
      begin
        if Assigned(act.CoreParser) then
          Result := act.CoreParser.GetVarValue(FldName);
      end;
    end;
  end;
begin
  Result := '';
  sText := '';

  PrintDocName := LowerCase(PrintDocName);
  Try
    dbm.gf.dodebug.msg('Processing the Digital signature...');
   //Fetch the AxDSignType
    AxDSignType := '';
    if assigned(dbcall) then DSignTypeFld := dbcall.struct.GetField('axdsigntype');
    if DSignTypeFld <> nil then
    begin
      AxDSignType := lowercase(GetValue('axdsigntype'));
    end;
    DSignStatus := '';
    if Assigned(act) then
      DSignStatus := act.DSignStatus;

    // Fetch the Config details
    xQry := dbm.GetXDS(nil);
    xQry.buffered := True;
    xQry.Close;
    sText := 'Select DocNameField, UserName from AxDSignConfig where STransId = :TransId '+
                            ' and '+dbm.gf.SQLLower+'(PrintDocName) = :PrintDocName and OrdNo = 1 ';
    if (DSignTypeFld <> nil) then
      sText := sText + ' and '+dbm.gf.SQLLower+'(doctype) = :DocType ';
    xQry.CDS.CommandText := sText;
    xQry.AssignParam(0,TransId,'c');
    xQry.AssignParam(1,PrintDocName,'c');
    if (DSignTypeFld <> nil) then
      xQry.AssignParam(2,AxDSignType,'c');
    xQry.Open;
    if xQry.CDS.RecordCount = 0 then
    begin
      Result := '';
      Exit;
    end;

    Try
      //sText   := xQry.CDS.FieldByName('SourcePath').AsString;
      sText   := GetCurrentDir + '\DigiSign\'+TransId+'\'+ xQry.CDS.FieldByName('UserName').AsString;
      DocName := xQry.CDS.FieldByName('DocNameField').AsString;
      DocName := GetValue(DocName);

      // Copy the File
      DocName := dbm.gf.FindAndReplace(DocName, '/', '_');
      DocumentName := DocName + ExtractFileExt(FileName);
      DocName := sText + '\'+DocName + ExtractFileExt(FileName);
      DSignResult := '';
      dbm.gf.dodebug.msg('DSignStatus value: '+ DSignResult);
      if (DSignStatus = '') then
      begin
        DSignResult := CheckFileExistence(TransId,DocumentName);
        if Trim(DSignResult) <> '' then   // If already exists then exit;
          goto StopProc;
      end;
      if (DSignStatus = 'withdraw') or (DSignStatus = 'overwrite') then
        DeleteExistence(TransId,DocumentName);
      if (DSignStatus = 'withdraw') then  // If value=withdraw then delete and exit;
        goto StopProc;

      if not DirectoryExists(ExtractFilePath(DocName)) then
        ForceDirectories(ExtractFilePath(DocName));
      CopyFile(PChar(FileName), PChar(DocName), true);
      dbm.gf.dodebug.msg('Document name : '+ DocumentName);

      // Make entries into Trans table
      xQry.Close;
      with xQry.CDS do
      begin
        xQry.ClearParams;
        sText := 'Select * from AxDSignConfig where STransId = :TransId and '+
                  dbm.gf.SQLLower+'(PrintDocName) = :PrintDocName ';
        if (DSignTypeFld <> nil) then
          sText := sText + ' and '+dbm.gf.SQLLower+'(DocType) = :DocType ';
        sText := sText + ' order by OrdNo ';
        CommandText := sText;
        xQry.AssignParam(0,TransId,'c');
        xQry.AssignParam(1,PrintDocName,'c');
        if (DSignTypeFld <> nil) then
          xQry.AssignParam(2,AxDSignType,'c');
        Open;
        SendMailFlag := false; SendSMSFlag := false;
        AxpMailId := ''; MailId := ''; Subject := ''; Content := '';
        while not Eof do
        begin
          ServerDate := dbm.gf.ConvertToDBDateTime(dbm.connection.dbtype,dbm.getserverdatetime);
          ServerDate:= dbm.gf.findandreplace(dbm.gf.dbdatestring, ':value', ServerDate);
          Status := '0';
          If FieldByName('OrdNo').AsInteger = 1 then
          begin
            Status := '1';
            if lowercase(FieldByName('SendMail').AsString) = 'y' then
            begin
              SendMailFlag := True;
              MailId       := FieldByName('MailId').AsString;
            end;
            If lowercase(FieldByName('SendSMS').AsString) = 'y' then
            begin
              SendSMSFlag := True;
              MobileNo    := FieldByName('MobileNo').AsString;
            end;
            AxpMailId := FieldByName('AxpMailId').AsString;
          end;
          sText := 'Insert into AxDSignTrans (STransId, DocDate, DocumentName, UserName, OrdNo, '+
                   'Status, DocType, AxpMailId, MailId, MobileNo, SendMail, SendSMS, MailStatus, SMSStatus) '+
                   'values ('+QuotedStr(FieldByName('STransId').AsString)+','+ServerDate+', '+ QuotedStr(ExtractFileName(DocName)) +
                   ',' + Quotedstr(FieldByName('UserName').AsString) + ',' + FieldByName('OrdNo').AsString +
                   ', '+Status+', '+QuotedStr(FieldByName('DocType').AsString) + ', ' +FieldByName('AxpMailId').AsString +
                   ', '+QuotedStr(FieldByName('MailId').AsString) + ', '+QuotedStr(FieldByName('MobileNo').AsString) +
                   ', '+QuotedStr(FieldByName('SendMail').AsString) + ', '+QuotedStr(FieldByName('SendSMS').AsString) +
                   ', 0,0)' ;
          AxProvider.ExecSQL(sText,'','',False);
          Next;
        end;

        if (SendMailFlag) or (SendSMSFlag) then
        begin
          dbm.gf.dodebug.msg('Checking the Mail configuration table');
          xQry.ClearParams;
          sText := 'Select * from AxDSignMail where AxpMailId = '+ AxpMailId;
          xQry.CDS.CommandText := sText;
          xQry.Open;
          if (xQry.CDS.RecordCount > 0) and (SendMailFlag) and (Assigned(act)) then
          begin
            dbm.gf.dodebug.msg('Processing the Mail...');
            Subject := xQry.CDS.FieldByName('Subject').AsString;
            Content := xQry.CDS.FieldByName('MailContent').AsString;
            xml := LoadXMLData('<root></root>');
            mailnode:= xml.DocumentElement.AddChild('sendmail');
            //if assigned(cxds.findfield('mailfrom')) then
            //  mailfrom := trim(getusermailid(cxds.CDS.FieldByName('mailfrom').AsString));
            mailnode.AddChild('mailfrom').NodeValue:= trim(getusermailid(dbm.gf.username));
            mailnode.AddChild('mailto').NodeValue  := MailId;
            mailnode.AddChild('cc').NodeValue      := '';
            mailnode.AddChild('subject').NodeValue := Subject;
            mailnode.AddChild('body').NodeValue    := Content;
            mailnode.AddChild('attach').NodeValue  := '';
            mailnode.AddChild('msoutlook').NodeValue := '';
            mailnode.AddChild('edattach').NodeValue := '';
            Result := act.SendMail(mailnode);
            dbm.gf.dodebug.msg('Mail response : '+ Result);
            if (Result = '') then
            begin
              sText := 'update AxDSignTrans set MailStatus = 1 where OrdNo = 1 and AxpMailId = ' + AxpMailId +
                        ' and stransid = '+ QuotedStr(TransId) + ' and documentname = '+ QuotedStr(DocumentName);
              AxProvider.ExecSQL(sText,'','',False);
              dbm.gf.dodebug.msg('Mail status has been updated');
            end;
          end;
          if (xQry.CDS.RecordCount > 0) and (SendSMSFlag) then
          begin
            dbm.gf.dodebug.msg('Processing the SMS...');
            Content := xQry.CDS.FieldByName('SMSContent').AsString;
            if dbm.Connection.dbType='oracle' then
              sText := 'insert into AxSMS (recordid, mobileno,msg,status) (SELECT AxSMS_Seq.NEXTVAL,' + QuotedStr(MobileNo) +
                        ', '+ QuotedStr(Content) + ', 0 from dual)'
            else
              sText := 'insert into AxSMS (mobileno,msg,status) values (' + QuotedStr(MobileNo) +
                      ', '+ QuotedStr(Content) + ', 0)';
            AxProvider.ExecSQL(sText,'','',False);
            dbm.gf.dodebug.msg('SMS data inserted into table.');
          end;
        end;

      end;
      Result := '';
      if Assigned(act) and (DSignStatus = '') then
        act.DSignMsg := 'Document has been sent for digital signature.';
      StopProc:
      begin
        if Assigned(act) and (DSignResult <> '') then
        begin
          if act.DSignResult = '' then
            act.DSignResult := DSignResult
          else
            act.DSignResult := act.DSignResult + ','+ DSignResult;
          dbm.gf.dodebug.msg('DSign process result: '+ act.DSignResult);
        end;
      end;
      dbm.gf.dodebug.msg('Done the Digital signature process.');  //9.6 1
    Except
      On E:Exception do
        Result := E.Message;
    End;
  Finally
    if Assigned(xQry) then
      FreeAndNil(xQry);
  End;

end;

function TASBDataObj.getusermailid (username: String):String;
var
  uxds : TXDS;
  uname : String;
begin
  result := '';
  try
    uxds := dbm.GetXDS(nil);
    uxds.buffered := true;
    uxds.CDS.CommandText := 'select email from axusers where username = '+ QuotedStr(username);
    uxds.open;
    if uxds.cds.RecordCount > 0 then begin
      uname := uxds.CDS.FieldByName('email').AsString;
    end;
    uxds.close;
    result := trim(uname);
  finally
    FreeAndNil(uxds);
  end;
end;

procedure TASBDataObj.SubmitClientValuesToSD_New;
var j,fno,rno,rowno,LastRowNo,LastFrameNo,pfno,prno:integer;
    f,v,dt,srecid, ov,s,encrypt_val:String;
    n,rnode : ixmlnode;
    id,oldid,recid : extended;
    fld : pFld;
begin
  dbcall.StoreData.webdata := true;
  dbcall.storedata.ClientNotify:=false;
  dbm.gf.dodebug.msg('Submiting & Registering data ');
  RegisterGlobalVars;
  fno:=0;rno:=0;
  s:=vartostr(xmldoc.DocumentElement.Attributes['frameno']);
  if s<>'' then fno := strtoint(s);
  s:=vartostr(xmldoc.DocumentElement.Attributes['activerow']);
  if s<>'' then rno := strtoint(s);
  if vartype(xmldoc.DocumentElement.Attributes['pdc']) <> varnull then
  begin
     if vartostr(xmldoc.DocumentElement.Attributes['pdc']) <> '' then
        pfno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['pdc']));
  end;
  if vartype(xmldoc.DocumentElement.Attributes['prow']) <> varnull then
  begin
     if vartostr(xmldoc.DocumentElement.Attributes['prow']) <> '' then
        prno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['prow']));
  end;
  rnode := xmldoc.DocumentElement.ChildNodes.FindNode('data'); // his node used for savedata service
  if rnode = nil then rnode := xmldoc.DocumentElement.ChildNodes.FindNode('FieldList'); // This node used for dofillgridvalues service
  if rnode = nil then n := xmldoc.DocumentElement.ChildNodes.FindNode('varlist'); // his node used for action service
  if assigned(n) then rnode := n.ChildNodes[0];
  if rnode = nil then rnode := xmldoc.DocumentElement;
  id := 0; oldid := 0; recid := 0;LastRowNo:=-1; LastFrameNo := -1;
  for j:=0 to rnode.childnodes.count-1 do begin
    n := rnode.childnodes[j];
    f:=vartostr(n.NodeName);
    if (f = 'globalvars') or (f = 'uservars') then continue;
    v:=vartostr(n.NodeValue);
    if (depCall) and (v='') then continue;
    //This section reads the axp_recid field from XML into recid variable. This is used as recordid for all subsequent elements.
    if copy(f,1,9) = 'axp_recid' then  begin
       recid := dbm.gf.strtofloatz(v);
       srecid := v;
       continue;
    end;
    //Convert special characters to actual characters.

    if pos('<br>',v) > 0  then v := dbm.gf.FindAndReplace(v,'<br>',#$D#$A);
    ov:='';oldid:=0;encrypt_val := '';
    fld:=dbcall.struct.GetField(f);
    if assigned(fld) then
    begin
      //submit to storedata.
      id := dbm.gf.strtofloatz(vartostr(n.attributes['id']));
      oldid := dbm.gf.strtofloatz(vartostr(n.attributes['oldid']));
      dt := fld.DataType;
      if vartostr(n.Attributes['rowno']) <> '' then
         rowno := strtoint(vartostr(n.Attributes['rowno']))
      else rowno := 1;
      if rowno = 0 then rowno := 1;
      ov:=vartostr(n.attributes['ov']);
      if vartostr(n.Attributes['multiselect']) = 'true' then
         fld.cl_multiselect := true;
      if vartostr(n.Attributes['separator']) = '' then
         fld.Separator := ','
      else
         fld.Separator := vartostr(n.Attributes['separator']);
      if fld.EncryptValue then encrypt_val := vartostr(n.Attributes['env']);
      if not fld.AsGrid then
        dbcall.Parser.RegisterVar(fld.FieldName, dt[1], v)
      else if ((fno=fld.FrameNo) and (rowno=rno)) or ((pfno=fld.FrameNo) and (rowno=prno) ) then
        dbcall.Parser.RegisterVar(fld.FieldName, dt[1], v);

      if ((fld.FrameNo <> lastframeno) or (rowno <> LastRowNo)) and (recid>0)  then
      begin
         if fld.frameno=1 then
         begin
            dbcall.Validate.Parser.RegisterVar('recordid', 'n', srecid);
            dbcall.StoreData.SetRecid(recid);
         end;
         dbcall.storedata.RecordIdList.Add(dbcall.struct.axprovider.dbm.gf.Pad(IntToStr(fld.frameno), 3, ' ')
           + dbcall.struct.axprovider.dbm.gf.Pad(IntToStr(rowno), 5, ' ') + dbcall.struct.axprovider.dbm.gf.pad(floattostr(recid),15,' ')+fld.Tablename);
         LastRowNo := rowno;
         LastFrameNo := fld.FrameNo;
      end;
      if dbcall.StoreData.LastSavedRecordId > 0 then
      begin
        if oldid = 0  then oldid := id;
        if ov <> '' then
        begin
          if pos('<br><br>',ov) > 0  then ov := dbm.gf.FindAndReplace(ov,'<br><br>',#$D#$A);
          if pos('<br>',ov) > 0  then ov := dbm.gf.FindAndReplace(ov,'<br>',#$D);
        end else begin
          if not n.HasAttribute('ov') then
            ov := v;
        end;
      end;
      dbcall.StoreData.DirectSubmitValue(fld, v, ov, rowno, id, oldid, recid,encrypt_val);
    end else begin
      if vartostr(n.Attributes['t']) <> '' then
         dt := vartostr(vartostr(n.Attributes['t']))
      else dt := 'c';
      dbcall.Parser.RegisterVar(f,dt[1],v);
    end;
  end;
  dbcall.storedata.ClientNotify:=true;
  dbm.gf.dodebug.msg('Setting values over');
  dbcall.TrimExtraRows;
  dbcall.StoreData.ins_del_rows.Clear;
  dbcall.StoreData.webdata := false;
  if DataSubmitSort then
     dbcall.struct.MakeProperStartIndex;
  for j := 0 to dbcall.StoreData.structdef.frames.Count-1 do begin
     if dbcall.StoreData.RowCountForWeb(pFrm(dbcall.StoreData.structdef.frames[j]).FrameNo) > 0 then
        pFrm(dbcall.StoreData.structdef.frames[j]).HasDataRows := True;
  end;
  dbcall.Validate.FastDataFlag := FastDataFlag;
end;

function TASBDataObj.GridToJSONNew(FrameNo:integer; WithDropDown, OnlyChanged:Boolean):integer;
var i,j, k, FirstSavedField, rcount, r , rno :integer;
    fm:pFrm;
    fd:pFld;
    frec:pFieldRec;
    s,v,dtype : string;
begin
  dbm.gf.dodebug.msg('Grid To JSON '+Inttostr(FrameNo));
  fm:=dbcall.Struct.GetFrame(FrameNo);
  rcount:=dbcall.StoreData.GetRowCount(fm.FrameNo);
  rno := rcount - existing_gridrows;
  if rcount=0 then rcount:=1;
  dbm.gf.dodebug.msg('Row Count '+Inttostr(rcount));
  dbm.gf.dodebug.msg('Existing Row Count '+Inttostr(existing_gridrows));
  if not RefreshGrid then
  begin
    for r := 1 to rcount do begin
  //    dbcall.validate.RegRow(FrameNo, r);
      if (existing_gridrows > 0)  and (r <= existing_gridrows) then continue;
      for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
        fd:=pFld(dbcall.struct.flds[j]);
        if fd.FrameNo <> fm.FrameNo  then  continue;
        frec:=dbcall.storedata.GetFieldRec(fd.FieldName, r);
        if (onlyChanged) and assigned(frec) and (not frec.ClientNotify) then continue;
        if (not assigned(frec)) or (frec.Value = '') then
        begin
           if fd.FieldName = 'dc' + inttostr(fd.FrameNo) + '_image' then
           begin
            if fm.jsonstr = '' then
            begin
              DCToJSON(fd.FrameNo,-1);
            end;
            fm.jsonstr := fm.jsonstr + '{';
            dtype := 's';
            v := '';
            fm.jsonstr := fm.jsonstr + '"n":"'+fd.FieldName+'","v":"'+v+'"';
            fm.jsonstr := fm.jsonstr + ',"r":"'+inttostr(r)+'"';
            fm.jsonstr := fm.jsonstr + ',"t":"'+dtype+'"';
            fm.jsonstr := fm.jsonstr + '},';
            continue;
           end else continue;
        end;
        if (fd.DataType = 'i') or (fd.FieldName = 'dc' + inttostr(fd.FrameNo) + '_image') then
        begin
          if fd.Tablename <> '' then
             ImageFieldToJSON(fd,'',frec,inttostr(r))
          else
          begin
            If (fd.Exprn >= 0) Then
            begin
              If Dbcall.Parser.EvalPrepared(fd.Exprn) Then
              begin
                v := Dbcall.Parser.Value;
                if v <> '' then
                  ImageFieldToJSON(fd,v,frec,inttostr(r))
              end;
            end;
          end;
        end else FieldToJSON(fd, frec,rno);
      end;
    end;
  end else
  begin
    for r := 1 to rcount do begin
      dbcall.validate.RegRow(FrameNo, r);
      dbm.gf.dodebug.msg('Start Index '+Inttostr(fm.StartIndex));
      dbm.gf.dodebug.msg('End Index '+Inttostr(fm.StartIndex+fm.FieldCount - 1));
      for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
        fd:=pFld(dbcall.struct.flds[j]);
        if fd.FrameNo <> fm.FrameNo  then  continue;
        s := Dbcall.validate.Prepare_Selected_Field(fd,VisibleDCs) ;
        FieldToJSONNew(fd,s);
      end;
    end;
  end;
  result:=fm.StartIndex+fm.FieldCount;
  if rcount=1 then ChangeHasDataRowStatus(fm);
end;

procedure TASBDataObj.FieldToJSONNew(fd:pfld ; frec:string;rno:integer=-1);
var i:integer;
    n, v, dtype, dpm, cv, mr, rowstring :String;
    q : TXDS;
    fm : pFrm;
    value,rowno,idvalue,oldidvalue : string;
begin
  value := dbm.gf.GetNthString(frec,1,'`');
  if fd.ComponentType = '' then
  begin
    if not depCall then
    begin
      if value = '' then  exit;
      if (not dbm.gf.MobileWSFlag) then
          if (fd.DataType = 'n') and (StrToFloat(value) = 0) then exit;
    end;
  end;
  fm:=dbcall.Struct.GetFrame(fd.FrameNo);
  if (fm.PopIndex > -1) and (not fm.HasDataRows)then exit;
  rowno := dbm.gf.GetNthString(frec,2,'`');
  idvalue := dbm.gf.GetNthString(frec,3,'`');
  oldidvalue := '0';
  if fm.jsonstr = '' then
  begin
    DCToJSON(fd.FrameNo,rno);
  end;
  dbm.gf.dodebug.msg('Field to JSON '+fd.fieldname+' row '+RowNo);
  q:=fd.QSelect;
  n:=fd.FieldName;
  v:=value;
  if fd.asgrid then rowstring:=rowno else rowstring:='0';
  fm.jsonstr := fm.jsonstr + '{';

  if pos(#$D#$A,v) > 0  then v := dbm.gf.FindAndReplace(v,#$D#$A,'<br>');
  if pos(#$A,v) > 0  then v := dbm.gf.FindAndReplace(v,#$A,'<br>');
  if pos(#$D,v) > 0  then v := dbm.gf.FindAndReplace(v,#$D,'<br>');
  v:=dbm.gf.FindAndReplace(v, '"', '^^dq');
  if fd.DataType = 't' then
     dtype := 'm'
  else  if fd.DataType = 'i' then
     dtype := 'i'
  else if (fd.ModeofEntry = 'select') and (fd.txtSelection <> true) then
  begin
     dtype := 'c';
     if fd.ComponentType <>'' then dtype:=fd.ComponentType;
  end else dtype := 's';
  fm.jsonstr := fm.jsonstr + '"n":"'+n+'","v":"'+v+'"';
  fm.jsonstr := fm.jsonstr + ',"r":"'+rowstring+'"';
  cv:='';
  if assigned(q) then begin
    if (not dbm.gf.CopyTrans and fd.HasGridParents) or (not fd.AsGrid) then mr:='0' else mr:='1';
    if fd.FromList then mr:='1';
    if (fd.ModeofEntry = 'select') then
    begin
      if fd.SourceKey then fm.jsonstr := fm.jsonstr + ',"idcol":"yes"' + ',"id":"'+ IdValue +'"' + ',"oldid":"'+ OldIdValue+'"'
      else fm.jsonstr := fm.jsonstr + ',"idcol":"no"'+ ',"id":"'+ IdValue +'"' + ',"oldid":"'+ OldIdValue+'"' ;
      if fd.AsGrid then fm.jsonstr := fm.jsonstr + ',"mr":"'+mr+'"';
    end;
  end ;
  if fd.ComponentType <>'' then
  begin
    dpm:='';
    if assigned(fd.Dependents) then begin
      for i := 0 to fd.dependents.count-1 do begin
        if fd.dependenttypes[i+1]='f' then begin
          dpm:=dpm + fd.dependents[i]+',';
        end;
      end;
    end;
    if dpm<>'' then delete(dpm, length(dpm), 1);
    if fd.ComponentType <>'' then cv := DropDownToJSON(fd, dpm);
    if dpm<>'' then
      fm.jsonstr := fm.jsonstr + ',"dpm":"'+dpm+'"';
  end;
  fm.jsonstr := fm.jsonstr + ',"t":"'+dtype+'"';
  if cv = '' then
     fm.jsonstr := fm.jsonstr + '},'
  else begin
    fm.jsonstr := fm.jsonstr +'},' + cv  ;
  end;
end;

procedure TASBDataObj.LoadDataFieldToJSON(fd:pfld; frec:pFieldRec);
var i:integer;
    n, v, dtype, src, dpm, cv, mr, rowstring :String;
    q : TXDS;
    fm : pFrm;
begin
  if fd.ComponentType = '' then
     if not assigned(frec) or ((not depCall) and (frec.value = '')) then  exit;
  fm:=dbcall.Struct.GetFrame(fd.FrameNo);
  if (fm.PopIndex > -1) and (not fm.HasDataRows)then exit;
  if fm.jsonstr = '' then
  begin
    DCToJSON(fd.FrameNo,-1);
  end;
  dbm.gf.dodebug.msg('Field to JSON '+frec.fieldname+' row '+inttostr(frec.RowNo));
  q:=fd.QSelect;
  n:=fd.FieldName;
  v:=frec.value;
  if fd.asgrid then rowstring:=inttostr(frec.rowno) else rowstring:='0';
  fm.jsonstr := fm.jsonstr + '{';

  if pos(#$D#$A,v) > 0  then v := dbm.gf.FindAndReplace(v,#$D#$A,'<br>');
  if pos(#$A,v) > 0  then v := dbm.gf.FindAndReplace(v,#$A,'<br>');
  if pos(#$D,v) > 0  then v := dbm.gf.FindAndReplace(v,#$D,'<br>');
  v:=dbm.gf.FindAndReplace(v, '"', '^^dq');
  if frec.DataType = 'n' then
  begin
    DbCall.Validate.fld := fd;
    v := DbCall.Validate.FormatNumber(v);
  end;
  if fd.DataType = 't' then
     dtype := 'm'
  else  if fd.DataType = 'i' then
     dtype := 'i'
  else if (fd.ModeofEntry = 'select') and (fd.txtSelection <> true) then
  begin
     dtype := 'c';
     if fd.ComponentType <>'' then dtype:=fd.ComponentType;
  end else dtype := 's';
  fm.jsonstr := fm.jsonstr + '"n":"'+n+'","v":"'+v+'"';
  fm.jsonstr := fm.jsonstr + ',"r":"'+rowstring+'"';
  cv:='';
  if assigned(q) then begin
    if (fd.HasGridParents) or (not fd.AsGrid) then mr:='0' else mr:='1';
    if fd.FromList then mr:='1';
    if (fd.ModeofEntry = 'select') then
    begin
      if fd.SourceKey then fm.jsonstr := fm.jsonstr + ',"idcol":"yes"' + ',"id":"'+ floattostr(frec.IdValue) +'"' + ',"oldid":"'+ floattostr(frec.OldIdValue)+'"'
      else fm.jsonstr := fm.jsonstr + ',"idcol":"no"'+ ',"id":"'+ floattostr(frec.IdValue) +'"' + ',"oldid":"'+ floattostr(frec.OldIdValue)+'"' ;
      if fd.AsGrid then fm.jsonstr := fm.jsonstr + ',"mr":"'+mr+'"';
    end;
  end;
  if fd.ComponentType <>'' then
  begin
    dpm:='';
    if assigned(fd.Dependents) then begin
      for i := 0 to fd.dependents.count-1 do begin
        if fd.dependenttypes[i+1]='f' then begin
          dpm:=dpm + fd.dependents[i]+',';
        end;
      end;
    end;
    if dpm<>'' then delete(dpm, length(dpm), 1);
    cv := DropDownToJSON(fd, dpm);
    if dpm<>'' then
      fm.jsonstr := fm.jsonstr + ',"dpm":"'+dpm+'"';
  end;
  fm.jsonstr := fm.jsonstr + ',"t":"'+dtype+'"';
  if cv = '' then
     fm.jsonstr := fm.jsonstr + '},'
  else begin
    fm.jsonstr := fm.jsonstr +'},' + cv  ;
  end;
end;

procedure TASBDataObj.QuickChangeHasDataRowStatus(fm : pFrm);
  var i,ValidRowPos : integer;
  s,s1 : string;
begin
  if (not assigned(fm)) then exit;
  if (fm.jsonstr ='') then exit;
  if (not fm.AsGrid) then exit;
  s := '';s1:='';
  if not fm.HasDataRows then
  begin
    ValidRowPos := dbcall.StoreData.GetFieldIndex('validrow'+inttostr(fm.FrameNo), 1);
    if ((validrowpos >= 0) and (uppercase(pFieldRec(dbcall.StoreData.FieldList[validrowpos]).Value)='T')) then
    begin
       i := pos('"hasdatarows"',fm.jsonstr)+20;
       s := copy(fm.jsonstr,1,i);
       s1 := s;
       delete(fm.jsonstr,1,i);
       s := dbm.gf.FindAndReplace(s,'no','yes');
       fm.jsonstr :=  s + fm.jsonstr;
    end;
  end;
  if s1 = '' then
  begin
    i := pos('"hasdatarows"',fm.jsonstr)+20;
    s1 := copy(fm.jsonstr,1,i);
  end;
  if pos('"cr',s1) = 0 then
  begin
     i := pos('"t"',fm.jsonstr)-1;
     Insert(',"cr":"i1"',fm.jsonstr,i);
  end;
end;

Function TASBDataObj.IsForIviewDef(x : IXMLDocument) : Boolean;
var enode,tempnode : IXMLNode;
begin
  Result := False;
  enode := x.DocumentElement;
  if enode.ChildNodes.FindNode('varlist') <> nil then
  begin
    tempnode := enode.ChildNodes.FindNode('varlist');
    if tempnode.ChildNodes.FindNode('row') <> nil  then
    begin
      tempnode := tempnode.ChildNodes.FindNode('row');
      if tempnode.ChildNodes.FindNode('iviewparams') <> nil  then
      begin
        Result := True;
        ivParamList := VarToStr(tempnode.ChildNodes.FindNode('iviewparams').NodeValue);
      end
      else if tempnode.ChildNodes.FindNode('SQL_editor_paramsql') <> nil then
      begin
        Result := True;
        ivParamList := GetIviewParams(VarToStr(tempnode.ChildNodes.FindNode('iname').NodeValue),
                       VarToStr(tempnode.ChildNodes.FindNode('SQL_editor_paramsql').NodeValue));
      end;
    end;
  end;
end;

Function TASBDataObj.SetValueForScript(FieldValues : String) : String;
var fn, Fv, r,s,v,oldval,fldList,FormLoadFldList,fldData : String;
    i,rn,j,fno,k : Integer;
    flist,FldValList : TStringList;
    fld : pFld;
begin
    dbm.gf.dodebug.Msg('Set Value Function In Script');
    rn := 0;
    fno := 0;
    FormLoadFldList := '';
    j := 0;
    fldList := ','+FormLoadFldList+',';
    flist := TStringList.Create;
    FldValList := TStringList.Create;
    FldValList.Delimiter := '|';
    FldValList.DelimitedText := FieldValues;
	
    for I := (FldValList.Count-1) downto 0 do
    begin
      fldData := FldValList[i];
      dbm.gf.dodebug.Msg('Row No :' + inttostr(i));
      fn := dbm.gf.GetNthString(fldData, 1, '=');
      j := j + 1 ;
      fv := dbm.gf.GetNthString(fldData, 2, '=');
      if copy(fv,1,1) = ':' then
      begin
        delete(fv,1,1);
        fv := act.GetParamValue(fv);
      end;
      j := j + 1;
      r := dbm.gf.GetNthString(fldData, 3, '=');
      if copy(r,1,1) = ':' then
         rn := strtoint(act.GetParamValue(r))
      else if r <> '' then
         rn := strtoint(r);
      j := j + 1;
      dbm.gf.dodebug.Msg('Field Name :' + fn);
      dbm.gf.dodebug.Msg('Field value :' + fv);
      dbCall.Storedata.SubmitValue(fn,rn, fv, '', 0, 0, 0);
      if pos(','+fn+',' , fldList) = 0 then
      begin
         if FormLoadFldList = '' then FormLoadFldList := fn
         else FormLoadFldList := FormLoadFldList + ',' + fn;
      end;
      fld:=dbcall.struct.GetField(fn);
      fno := fld.FrameNo;
      if (fno > 1) and (rn > 1) then
         RegisterActiveRow(fno,rn)
      else
         dbcall.Parser.RegisterVar(fn,fld.DataType[1],fv);
         GetDepentendFldList(fn,flist);
    end;

    dbm.gf.dodebug.Msg('Refreshing values in depend field');
    for i := 0 to flist.Count - 1 do
    begin
      s := flist.Strings[i];
      fld:=dbcall.struct.GetField(s);
      if not assigned(fld) then continue;
      if pos(','+s+',' , fldList) = 0 then FormLoadFldList := FormLoadFldList + ',' + s;
      if fld.txtSelection = true then
      begin
        if fld.Exprn > -1 then
        begin
          If DbCall.Parser.EvalPrepared(fld.Exprn) Then
            v := DbCall.Parser.Value;
          DbCall.StoreData.SubmitValue(fld.FieldName, 1, v, '', 0, 0, 0);
          DbCall.Parser.RegisterVar(fld.FieldName,fld.DataType[1],v);
        end ;
        continue;
      end;
      if not fld.AsGrid then
      begin
         v := DbCall.Validate.RefreshField(fld,1);
      end else begin
        j := dbcall.StoreData.GetRowCount(fld.FrameNo);
        if j > 0  then
        begin
          if (fno = fld.FrameNo) then
          begin
             v := DbCall.Validate.RefreshField(fld,1) ;
          end else
          begin
             for k := 1 to j do
             begin
               DbCall.Validate.RegRow(fld.FrameNo,k);
               oldval := DbCall.Validate.Parser.GetVarValue(fld.FieldName);
               v := DbCall.Validate.RefreshField(fld,k) ;
               if v <> oldval then UpdateChangedRowsList(fld.FrameNo,k);
             end;
          end;
        end;
      end;
    end;
    if assigned(flist) then flist.Free;
end;

function TASBDataObj.CancelDataForScript(trnsid,rcdid,rem : String) : String;
var  tcstr,tid:String;
    f : extended ;
    rNode : ixmlnode;
    Newdbcall : tdbcall;
    newXML : IXMLDocument;
    IsCancelled,IsNewTrans : Boolean;
begin
    dbm.gf.dodebug.msg('Executing CANCEL from action');
    if ((act.transid = '') and (trnsid = '')) then raise exception.Create('Structure Name not defined for this action');
    IsNewTrans := False;
    IsCancelled := False;
    if copy(trnsid,1,1) = ':' then begin
      delete(trnsid,1,1);
      trnsid := DbCall.Validate.Parser.getvarvalue(trnsid);
    end;
    if copy(rcdid,1,1) = ':' then begin
      delete(rcdid,1,1);
      rcdid := DbCall.Validate.Parser.getvarvalue(rcdid);
    end;
    if trnsid <> '' then begin
      NewXML := nil;
      IsNewTrans := True;
      Newdbcall := TDbCall.Create;
      NewdbcaLL.axprovider := Axprovider;
      Newdbcall.Dbm := dbm;
      NewXML := Axprovider.GetStructureForScript('tstructs',trnsid,'','');
      Newdbcall.sxml := newXML;
      Newdbcall.transid := trnsid;
    end
    else begin
      if not assigned(dbCall) then begin
        dbm.gf.dodebug.msg('Creating dbcall');
        dbcall := TDbCall.Create;
        dbcaLL.axprovider := Axprovider;
        dbcall.Dbm := dbm;
        if act.stype = 'tstructs' then begin
          if trnsid = '' then dbcall.transid := act.sName;
        end;
      end;
    end;
    if rcdid = '' then tid :=  act.RecId
    else tid := rcdid;
    if tid = '' then tid := dbcall.parser.GetVarValue('recordid');
    f := dbm.gf.strtofloatz(tid);
    dbm.gf.dodebug.msg(' Recordid='+floattostr(f));
    if f>0 then begin
       if IsNewTrans then tcstr :=  CheckForAccess(Newdbcall.transid)
       else tcstr :=  CheckForAccess(dbcall.transid);
       if (tcstr = 'd') or (tcstr = '') then
       begin
          if IsNewTrans then Newdbcall.LoadData(f)
          else dbcall.LoadData(f);
          if IsNewTrans then IsCancelled := Newdbcall.storedata.cancelled
          else IsCancelled := dbcall.storedata.cancelled;
          if IsCancelled then
          begin
            act.createmsgnode('Transaction already cancelled');
            dbm.gf.dodebug.msg('Transaction already cancelled');
            exit;
          end;
          try
           dbm.StartTransaction(connection.ConnectionName);
           if IsNewTrans then Newdbcall.CancelTransaction(f,rem)
           else dbcall.CancelTransaction(f,rem);
           result := 'done';
           dbm.Commit(connection.ConnectionName);
          except on e:exception do
            begin
               dbm.RollBack(connection.ConnectionName);
               raise exception.Create(e.Message);
            end;
          end;
          result := 'done';
          act.createmsgnode('Transaction has been canceled');
       end else raise exception.Create('Access denied for deleting this transaction');
    end;
end;

function TASBDataObj.LoadDataInScript(s,pFieldValues : String) : String ;
var  tcstr, recid,dbtid:String;
     bkdbc:TDBCall;
     NewXML : IXMLDocument;
begin
    result := '';
    dbtid := '';
    dbm.gf.dodebug.msg('Executing load from action');
    if not assigned(dbCall) then
    begin
      dbcall := TDbCall.Create;
      dbcaLL.axprovider := Axprovider;
      dbcall.Dbm := dbm;
      dbcall.transid := s;
      bkdbc := nil;
    end else
    begin
      bkdbc:=dbcall;
      dbtid := dbcall.transid;
      if s <> dbcall.transid then begin
        NewXML := nil;
        dbcall := TDbCall.Create;
        dbcall.axprovider := Axprovider;
        dbcall.Dbm := dbm;
        NewXML := Axprovider.GetStructureForScript('tstructs',s,'','');
        dbcall.sxml := newXML;
        dbcall.transid := s;
        act.Moved_To_Other_Page := true;
      end;
    end;
    tcstr := '';
    tcstr := CheckForAccess(s);
    if (tcstr <> 'n') then begin
       recid := LoadDataForScript(s,pFieldValues);
       act.CoreParser.RegisterVar('recordid','c',recid);
       if (not act.Moved_To_Other_Page) and (actStructType <> 'iviews') and (recid <> '') then
       begin
         dbm.gf.dodebug.msg('Loading data from '+recid);
         if dbcall.struct.quickload then
         begin
            dbcall.LoadDataForWeb(strtofloat(recid)) ;
            act.RecId := recid;
            LoadDataByAction := true;
         end else dbcall.LoadData(strtofloat(recid));
       end;
    end else raise Exception.create('Access denied for this transaction');
    if (assigned(bkdbc)) and (bkdbc.transid <> s) then
    begin
      dbcall:=bkdbc;
      dbm.gf.dodebug.msg('dbcall reassigned');
    end;
    dbm.gf.dodebug.msg('Executing load from action over');
    result := dbtid;
end;

function TASBDataObj.LoadDataForScript(transid,pFieldValues : String) : String;
var ws,s,nv:String;
    x:txds;
    i:integer;
    xdoc: ixmldocument;
    n,n1 : IXMLNode;
    FieldList : TstringList;
begin
  dbm.gf.doDebug.Msg('Load Data in Script Function');
  ws := '';
  result := '';
  if (pFieldValues <> '') then begin
    FieldList := TstringList.Create;
    FieldList.Delimiter := '~';
    FieldList.StrictDelimiter := true;
    FieldList.DelimitedText := pFieldValues;
    for i:=0 to FieldList.count-1 do begin
      nv := FieldList.Names[i];
      if pos('.',nv) > 0 then
      begin
        if copy(nv,1,pos('.',nv)-1) <> transid then continue;
        delete(nv,1,pos('.',nv));
      end;

      if lowercase(nv) = 'recordid' then nv :=  dbCall.struct.PrimaryTable+'id';

      s:=Trim(FieldList.ValueFromIndex[i]);
      if copy(s,1,1) = ':' then begin
         delete(s,1,1);
         s := act.GetPValue(s);
      end;
      if s<>'' then
         ws:=ws + ' and '+nv+' = '+s;
    end;
    dbm.gf.doDebug.Msg('Where Condition : ' + ws);
  end;
  if ws<>'' then begin
    delete(ws, 1, 5);
    ws:='select ' + dbCall.struct.PrimaryTable+'id' + ' from '+ dbCall.struct.PrimaryTable+' where '+ws;
    x := axprovider.dbm.GetXDS(nil);
    x.buffered := True;
    x.CDS.CommandText:=ws;
    dbm.gf.doDebug.Msg('SQL : ' + ws);
    try
      xdoc:=axprovider.GetOneRecord(x.cds.CommandText, '', '');
    except on e : exception do
    begin
       x.close;
       ws := dbm.gf.FindAndReplace(ws,'=','like');
       x.CDS.CommandText:=ws;
       xdoc:=axprovider.GetOneRecord(x.cds.CommandText, '', '');
    end;
    end;
    if xdoc.DocumentElement.childnodes.count>0 then
    begin
      n := xdoc.DocumentElement.ChildNodes[0];
      if n <> nil then n1 := n.ChildNodes.findnode(uppercase(dbCall.struct.PrimaryTable+'id'));
      if n1 = nil then n1 := n.ChildNodes.findnode(dbCall.struct.PrimaryTable+'id');
      if n1 <> nil then
      begin
        result := vartostr(n1.NodeValue);
        dbm.gf.doDebug.Msg('Record to load : ' + result);
      end;
    end;
    x.close;
    x.free;
  end;
end;

function TASBDataObj.GetIviewParams(ivnm, sqltxt : String) : String;
var mxds : TXDS;
    params,dtype,res,prm,sparamtype : String;
    sl : TStringList;
    i : Integer;
begin
  params := dbcall.Parser.ExtractQueryParams(sqltxt);
  if params <> '' then
  begin
    res := '';
    mxds := axprovider.dbm.GetXDS(nil);
    mxds.buffered := True;
    sl := TStringList.Create;
    sl.Delimiter := ',';
    sl.StrictDelimiter := True;
    sl.DelimitedText := params;
    for I := 0 to sl.Count-1 do begin
      prm := sl[i];
      mxds.CDS.CommandText := 'select datatype,scripttext from dwb_iviewparams where '+axprovider.dbm.gf.sqllower+
      '(iname)='+QuotedStr(LowerCase(ivnm)) +' and '+axprovider.dbm.gf.sqllower+'(pname)='+QuotedStr(LowerCase(prm));
      mxds.open;
      if mxds.cds.RecordCount > 0 then begin
        dtype := mxds.cds.FieldByName('datatype').AsString;
        sparamtype := LowerCase(dtype[1]);
        res := res+','+prm+'~'+sparamtype;
      end;
    end;
  end;
  if res <> '' then Result :=  Copy(res,2,(length(res)-1))
  else Result := '';
end;

function TASBDataObj.DeleteDataInScript(trnsid,rcdid,showresult : String) : String;
var  tcstr,tid:String;
    f : extended;
    IsNewTrans,IsCancelled : Boolean;
    NewXML : IXMLDocument;
    Newdbcall : TDbCall;
begin
    dbm.gf.dodebug.msg('Executing Delete from action');
    if  ((trnsid = '') or (act.transid = '')) then raise exception.Create('Structure Name not defined for this action');
    IsNewTrans := False;
    IsCancelled := False;
    if copy(trnsid,1,1) = ':' then begin
      delete(trnsid,1,1);
      trnsid := DbCall.Validate.Parser.getvarvalue(trnsid);
    end;
    if copy(rcdid,1,1) = ':' then begin
      delete(rcdid,1,1);
      rcdid := DbCall.Validate.Parser.getvarvalue(rcdid);
    end;
    if trnsid <> '' then begin
      NewXML := nil;
      IsNewTrans := True;
      Newdbcall := TDbCall.Create;
      NewdbcaLL.axprovider := Axprovider;
      Newdbcall.Dbm := dbm;
      NewXML := Axprovider.GetStructureForScript('tstructs',trnsid,'','');
      Newdbcall.sxml := NewXML;
      Newdbcall.transid := trnsid;
    end
    else begin
      if not assigned(dbCall) then begin
        dbm.gf.dodebug.msg('Creating dbcall');
        dbcall := TDbCall.Create;
        dbcaLL.axprovider := Axprovider;
        dbcall.Dbm := dbm;
      if act.stype = 'tstructs' then dbcall.transid := act.sName;
      end;
    end;
    if rcdid = '' then tid :=  act.RecId
    else tid := rcdid;
    f := dbm.gf.strtofloatz(tid);
    dbm.gf.dodebug.msg(' Recordid='+floattostr(f));
    if f>0 then begin
       if IsNewTrans then tcstr :=  CheckForAccess(Newdbcall.transid)
       else tcstr :=  CheckForAccess(dbcall.transid);
       if (tcstr = 'd') or (tcstr = '') then
       begin
          if IsNewTrans then begin
            Newdbcall.LoadData(f);
            IsCancelled := Newdbcall.storedata.cancelled;
          end else begin
            dbcall.LoadData(f);
            IsCancelled := dbcall.storedata.cancelled;
          end;
          if IsCancelled then begin
            act.createmsgnode('Can not delete the cancelled transaction');
            dbm.gf.dodebug.msg('Can not delete the cancelled transaction');
            exit;
          end;
          try
           dbm.StartTransaction(connection.ConnectionName);
           if IsNewTrans then Newdbcall.DeleteData(f) else dbcall.DeleteData(f);
           if IsNewTrans then begin
             if Newdbcall.ErrorStr <> '' then
                result := Newdbcall.ErrorStr
             else begin
                dbm.Commit(connection.ConnectionName);
                result := 'done';
             end;
           end else begin
             if dbcall.ErrorStr <> '' then
                result := dbcall.ErrorStr
             else begin
                dbm.Commit(connection.ConnectionName);
                result := 'done';
             end;
           end;
          except on e:exception do begin
               dbm.RollBack(connection.ConnectionName);
               raise exception.Create(e.Message);
            end;
          end;
           act.createmsgnode('Transaction has been deleted');
       end else raise exception.Create('Access denied for deleting this transaction');
    end;
end;

function TASBDataObj.CancelDataForAction(rem : String) : String;
var  tcstr,tid:String;
    f : extended ;
    rNode : ixmlnode;
begin
    dbm.gf.dodebug.msg('Executing CANCEL from action');
    if  act.transid = '' then raise exception.Create('Structure Name not defined for this action');
    if not assigned(dbCall) then
    begin
      dbm.gf.dodebug.msg('Creating dbcall');
      dbcall := TDbCall.Create;
      dbcaLL.axprovider := Axprovider;
      dbcall.Dbm := dbm;
      if act.stype = 'tstructs' then dbcall.transid := act.sName;
    end;
    tid :=  act.RecId;
    if tid = '' then tid := dbcall.parser.GetVarValue('recordid');
    f := dbm.gf.strtofloatz(tid);
    dbm.gf.dodebug.msg(' Recordid='+floattostr(f));
    if f>0 then begin
       tcstr :=  CheckForAccess(dbcall.transid);
       if (tcstr = 'd') or (tcstr = '') then
       begin
          dbcall.LoadData(f);
          if dbcall.storedata.cancelled then
          begin
            act.createmsgnode('Transaction already cancelled');
            dbm.gf.dodebug.msg('Transaction already cancelled');
            exit;
          end;
          try
           dbm.StartTransaction(connection.ConnectionName);
           dbcall.CancelTransaction(f,rem);
           result := 'done';
           dbm.Commit(connection.ConnectionName);
          except on e:exception do
            begin
               dbm.RollBack(connection.ConnectionName);
               raise exception.Create(e.Message);
            end;
          end;
          result := 'done';
          act.createmsgnode('Transaction has been canceled');
       end else raise exception.Create('Access denied for deleting this transaction');
    end;
end;

//SubmitClientValuesToSDForPEG
procedure TASBDataObj.SubmitClientValuesToSDForPEG;
var j,fno,rno,rowno,LastRowNo,LastFrameNo,pfno,prno:integer;
    f,v,dt,srecid, ov,s,encrypt_val:String;
    n,rnode : ixmlnode;
    id,oldid,recid : extended;
    fld : pFld;
begin
  dbcall.StoreData.webdata := true;
  dbcall.storedata.ClientNotify:=false;
  dbm.gf.dodebug.msg('SubmitClientValuesToSDForPEG/Submiting & Registering data ');
  RegisterGlobalVars;
  fno:=0;rno:=0;
  s:=vartostr(xmldoc.DocumentElement.Attributes['frameno']);
  if s<>'' then fno := strtoint(s);
  s:=vartostr(xmldoc.DocumentElement.Attributes['activerow']);
  if s<>'' then rno := strtoint(s);
  if vartype(xmldoc.DocumentElement.Attributes['pdc']) <> varnull then
  begin
     if vartostr(xmldoc.DocumentElement.Attributes['pdc']) <> '' then
        pfno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['pdc']));
  end;
  if vartype(xmldoc.DocumentElement.Attributes['prow']) <> varnull then
  begin
     if vartostr(xmldoc.DocumentElement.Attributes['prow']) <> '' then
        prno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['prow']));
  end;
  rnode := xmldoc.DocumentElement.ChildNodes.FindNode('data'); // his node used for savedata service
  if rnode = nil then rnode := xmldoc.DocumentElement.ChildNodes.FindNode('FieldList'); // This node used for dofillgridvalues service
  if rnode = nil then n := xmldoc.DocumentElement.ChildNodes.FindNode('varlist'); // his node used for action service
  if assigned(n) then rnode := n.ChildNodes[0];
  if rnode = nil then rnode := xmldoc.DocumentElement;
  id := 0; oldid := 0; recid := 0;LastRowNo:=-1; LastFrameNo := -1;
  for j:=0 to rnode.childnodes.count-1 do begin
    n := rnode.childnodes[j];
    f:=vartostr(n.NodeName);
    v:=vartostr(n.NodeValue);
    //This section reads the axp_recid field from XML into recid variable. This is used as recordid for all subsequent elements.
    if copy(f,1,9) = 'axp_recid' then  begin
       recid := dbm.gf.strtofloatz(v);
       srecid := v;
       continue;
    end;
    //Convert special characters to actual characters.

    if pos('<br>',v) > 0  then v := dbm.gf.FindAndReplace(v,'<br>',#$D#$A);
    ov:='';oldid:=0;encrypt_val := '';
    fld:=dbcall.struct.GetField(f);
    if assigned(fld) then
    begin
      //submit to storedata.
      id := dbm.gf.strtofloatz(vartostr(n.attributes['id']));
      oldid := dbm.gf.strtofloatz(vartostr(n.attributes['oldid']));
      dt := fld.DataType;
      if vartostr(n.Attributes['rowno']) <> '' then
         rowno := strtoint(vartostr(n.Attributes['rowno']))
      else rowno := 1;
      if rowno = 0 then rowno := 1;
      ov:=vartostr(n.attributes['ov']);
      if vartostr(n.Attributes['multiselect']) = 'true' then
      begin
        fld.cl_multiselect := true;
        if vartostr(n.Attributes['separator']) = '' then
           fld.Separator := ','
        else
           fld.Separator := vartostr(n.Attributes['separator']);
      end;
      if fld.EncryptValue then encrypt_val := vartostr(n.Attributes['env']);
      if not fld.AsGrid then
        dbcall.Parser.RegisterVar(fld.FieldName, dt[1], v)
      else if ((fno=fld.FrameNo) and (rowno=rno)) or ((pfno=fld.FrameNo) and (rowno=prno) ) then
        dbcall.Parser.RegisterVar(fld.FieldName, dt[1], v);

      if ((fld.FrameNo <> lastframeno) or (rowno <> LastRowNo)) and (recid>0)  then
      begin
         if fld.frameno=1 then
         begin
            dbcall.Validate.Parser.RegisterVar('recordid', 'n', srecid);
            dbcall.StoreData.SetRecid(recid);
         end;
         dbcall.storedata.RecordIdList.Add(dbcall.struct.axprovider.dbm.gf.Pad(IntToStr(fld.frameno), 3, ' ')
           + dbcall.struct.axprovider.dbm.gf.Pad(IntToStr(rowno), 5, ' ') + dbcall.struct.axprovider.dbm.gf.pad(floattostr(recid),15,' ')+fld.Tablename);
         LastRowNo := rowno;
         LastFrameNo := fld.FrameNo;
      end;
      if dbcall.StoreData.LastSavedRecordId > 0 then
      begin
        if oldid = 0  then oldid := id;
        if ov <> '' then
        begin
          if pos('<br><br>',ov) > 0  then ov := dbm.gf.FindAndReplace(ov,'<br><br>',#$D#$A);
          if pos('<br>',ov) > 0  then ov := dbm.gf.FindAndReplace(ov,'<br>',#$D);
        end else begin
          if not n.HasAttribute('ov') then
            ov := v;
        end;
      end;
      //dbcall.StoreData.DirectSubmitValue(fld, v, ov, rowno, id, oldid, recid);
      dbcall.StoreData.SubmitValue(fld,fld.FieldName, rowno,v, ov, id, oldid, recid,encrypt_val);
    end else begin
      if vartostr(n.Attributes['t']) <> '' then
         dt := vartostr(vartostr(n.Attributes['t']))
      else dt := 'c';
      dbcall.Parser.RegisterVar(f,dt[1],v);
    end;
  end;

  dbcall.storedata.ClientNotify:=true;
  dbm.gf.dodebug.msg('Setting values over');
  dbcall.TrimExtraRows;
  dbcall.StoreData.ins_del_rows.Clear;
  dbcall.StoreData.webdata := false;
  dbcall.StoreData.FieldList.Sort(SortFieldListLoad);
  dbcall.StoreData.AfterSort;
  for j := 0 to dbcall.StoreData.structdef.frames.Count-1 do begin
     if dbcall.StoreData.RowCount(pFrm(dbcall.StoreData.structdef.frames[j]).FrameNo) > 0 then
        pFrm(dbcall.StoreData.structdef.frames[j]).HasDataRows := True;
  end;
  dbcall.Validate.FastDataFlag := FastDataFlag;
end;

end.

