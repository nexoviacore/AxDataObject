unit uStructDef;
{copied from  ver 11.1}
{Copied from Axpert9-XE3\Ver 11.2}

interface

Uses db, classes, forms, sysutils, uGeneralFunctions, Extctrls,
controls, Variants, XMLDoc, XMLIntf, uXDS, uAxprovider, uPropsXML,strutils,uParse;

type

  pfrm = ^TFrm;
  TFrm = record
    FrameNo, Parent, PageNo, PopParent, PopIndex, StartIndex, FieldCount , Rowcount, LastFieldNo : Integer;
    TableName, caption : String;
    AsGrid, Popup, AllowChange, ReadOnly,AllowEmpty,HasDataRows, AllowAddRow, AllowDeleteRow : Boolean; 
    Comp : TWinControl;
    ActRowDeps,jsonstr,mustloadsql : String;
  end;

  pFld = ^TFld;
  TFld = record
  FieldName, Tablename, DataType, ModeofEntry, Caption, LinkField, SourceField, Sourcetable, SourceTransid, CField,sgwidth, ComponentType,SelType : String;
  SQL,SearchSQL, displaydetail, prefixflds, Dependents : TStringList;
  DispName, Mask, Pattern, Hint, DetTransid,DetCondition,DepFrames, DependentTypes,RefOnChngFlds : String;
  FrameNo, Width, Dec,CurDec, DBDec, Exprn, ValExprn, orderno, searchcol,  sgheight, PopIndex , axRule_ValExprn , axRule_ValOnSave_ValExprn : Integer;
  SourceKey, AsGrid, NoDuplicate, Empty, Suggestive, hidden, Autoselect, readonly, multiline,ClientValidation, HasGridParents, HasHyperLink : Boolean;
  AllowChange, DisplayTotal, HasParams, SetCarry, Refresh, FromList,ApplyComma,OnlyPositive, DynamicParams,DispField,IsBtnField,IsParentField,RefreshOnChange,cl_multiselect: Boolean;
  cexp, cvalexp , AxRule_cvalexp , AxRule_ValOnSave_cvalexp : String;
  pwchar : AnsiChar;
  QSelect : TXDS;
  listvals, deps, detmap, pickfields, pickcaptions, DataRows : tstringlist;
  CommaSelection , txtSelection : Boolean;
  SaveValue,EncryptValue , CustomDecimal : Boolean;
  SequenceNode : IXMLNode;
  Tabstop:boolean;              // focus the read only field when the Tabstop is true
  FontStr,ColorStr : String;
  CustType : String;
  Separator : String;
  Kind : String;  // 9.5 1  // To store the control type whether horizontal or vertical
  UsedQuotedStr : Boolean;
  DepOrdNo: Extended;
  PickListMode : TPickListMode;
  PickListDef : String;
  PickRecFound : Boolean;
  PickListLevel : Integer;
  WordSearch : Boolean;
  FldUX : TStringList;
  end;

  pfg=^TFg;
  TFg=record
    fname , name, SQLText, colwidths, Groupfield,SelectOn,FooterStr,VExp, ParamNames : String;
    Map:TStringlist;
    Caps:TStringList;
    FromIView, MultiSelect, AutoShow, ValidateRow, HasParams, DynamicParams, ExecuteOnSave , firmbind, SelectAllRows : boolean;
    TargetFrame, SourceFrame,AddRows: integer;
    q : TXDS;
  end;

  pPopgrid=^TPopgrid;
  TPopgrid=record
    FrameNo,ParentFrameNo, PopAt:Integer;
  	popname,Heading,Parent,ParentField,Popcond,ShowButtons,inode,keyCols,DispField,SumFormat,Delimiter : String;
	  AutoShow,FirmBind,AutoFillFld : Boolean;
    AutoFill : TStringList;
  end;

  TStructDef = class
  private
    fg : pfg;
    frm : pfrm;
    popgrid : pPopGrid;
    fieldno,priorfrmno : integer;
    fXML,tmpSXML : IXMLDocument;
    rPurpose,oldfieldslist   : String ;
    CeatedFlds , CreatedDcs , FldCreaedOrder : TStringlist;
    ForDep : boolean;
    axRule_Validate , axRule_AllowEmpty , axRule_AllowDuplicate , axRule_ValidateOnSave : ixmlnode;

    procedure ClearFields;

    procedure Loaddef(pName, pCaption: String);
    procedure xloadstruct(x:ixmlnode);
    procedure xloaddc(x:ixmlnode);
    procedure xloadfield(x:ixmlnode);
    procedure xloadfillgrid(x:ixmlnode);
    procedure xloadgenmap(x:ixmlnode);
    procedure xloadmdmap(x:ixmlnode);
    procedure ConvertMOE;
    procedure SetXML(const Value: IXMLDocument);
    procedure GetDeps(fd:pFld; x:ixmlnode);
    procedure GetDetMap(fd:pFld; x:ixmlnode);
    procedure AddDeps(fd: pfld; dlist: String;fldindex:integer);
    function CreatePopGrid(x: ixmlnode): Integer;
    procedure AddToPopParentList(DepFrames, fldName: String);
    procedure GetPickLists(fd: pFld);
    procedure OrderDeps;
    procedure UpdateHasGridParents;
    procedure CreateOldFiledsList;
    procedure AddFgDeps(fd: pfld; dlist: String);
    procedure GetDispHyperDet(fd: pfld);
    procedure AddDcNameDeps(fd: pfld; dlist: String;fldidx:Integer);
    procedure AddLabelDeps(fd: pfld; dlist: String);
  	procedure GetDropDownPickLists(fd: pFld);
    procedure GetTableList;
    procedure Init(axprovide: TAxprovider; purpose: String);
    procedure xload_selected_dc(x : ixmlnode);
    function CreateSelectedFillGridDC(fldlist, visibleDCs, fgdc , fglist : string): string;
    procedure CreateMapFlds;
    procedure CreateParentFlds;
    procedure SetLoadDataXMLforWeb;
    procedure FloadForWeb(fldlist, visibleDCs, fglist,
      fillgriddc: string);
    procedure CreateSelectedDCFlds(dc,fldlist: AnsiString);
    function CreateSelectedFillGridDCForLoad(fldlist, visibleDCs, fgdc,
      fglist: string): string;
    procedure SetAxRulesNodes;
    procedure SetAxRulesProps(fname: string);
    procedure SetPEGandAmendFlag;
    procedure SetAxOtherSettings;

  public
    flds, frames, fgs, popgrids : TList;
    fld : pfld;
    ExprSetList,TrackFieldsList,ParentList,PopParentList, ImgFldList : TStringList;
    framenames, gridstring, NoChange, DocDateField, PrimaryTable, Caption, Transid, treetables, treeparents,tree,primaryfield : String;
    savecontrol, delcontrol, tables, PrimaryFields : String;
    fcount, framecount, ActiveRow, FailCount, ActiveFrame, ActiveField, NormalFrames : integer;
    propsxml : IXMLDocument;
    axprovider : taxprovider;
    track,TrackAllFields,HasDetStruct,attach, ListView, WorkFlow, Traceability , sdraft : boolean;
    LastFieldNo : Integer;
    SchemaName : String;
    MastFrameNos,stype:String;
    HasViewImgFlds,HasImage, HasImgPath : Boolean;
    NoEmptyImgFlds : String;
    track_chng : boolean;
    oldfields,TableList : TStringList;
    DynamicLabels : String;
    HasWorkFlow  , HasAutoGenFields , quickload , GetDependants , GetPList , QuickDataFlag : Boolean;
    HasPickListFlds : Boolean;
    SearchCond : String;
    visibleDCs,FgAutoShowMapFlds : AnsiString;
    Parser : TEval;
    FldLoadedDuringStructLoad , isPegAttached, IsAmendmentEnabled : Boolean;
    HasOutBound, HasAutoPrints, HasFormNotify : Boolean;
    bGenmapThroughQueue : Boolean;

    timezone_diff : Extended;
    Constructor Create(axprovide:TAxProvider; pName, pCaption, Purpose:String; x:IXMLDocument); overload;
    Constructor Create(axprovide:TAxProvider; pName, pCaption,Purpose:String); overload;
    constructor Create(axprovide:TAxProvider; pName :String; x:IXMLDocument);  overload;
    Destructor Destroy; override;
    function GetField(fieldname:String):pfld;
    function GetFieldIndex(fieldname:String):integer;
    function SearchDisplayName(s: String): pfld;
    function GetFrame(framename: String): pfrm; overload;
    function GetFrame(frameno: integer): pfrm; overload;
    function GetFirstFieldIndex(Frameno: Integer): integer;
    procedure SetLastEnabledFieldNo;
    function GetParents(fldname: String): String;
    function GetPopParents(frmno: Integer): String;
    function GetLastFieldIndex(Frameno: Integer): integer;
    function GetFirstEnabledFldName(Frameno: Integer): String;
    function GetFrameNo(fldname: String): integer;
    procedure AddToDynamicLabel(lblName: String);
    procedure CreateSelectedFlds(act,sname,fldlist ,visibleDCs,fglist,fillgriddc : string);
    function  CreateFgsInActions(fgname: ansistring) : pFg;
    procedure MakeProperStartIndex;
    procedure GetFrameForWebLoad(fno: ansistring);
    procedure xload_selected_field(x : ixmlnode);
    property XML : IXMLDocument read fXML write SetXML;
    procedure CreateSelectedFillGridDCOnDemand(fgdc, fglist: string);
  end;

Function SortFldsDefault(Item1, Item2: Pointer): Integer;
Function SortFldsLoad(Item1, Item2: Pointer): Integer;
Function SortFldsDeps(Item1, Item2: Pointer): Integer;

implementation


{ TStructDef }

constructor TStructDef.Create(axprovide: TAxProvider; pName,  pCaption , Purpose: String);
begin
  rPurpose :=  purpose ;
  create(axprovide, pName, pCaption,Purpose , nil);
end;

constructor TStructDef.Create(axprovide:TAxProvider; pName, pcaption, Purpose :String; x:IXMLDocument);
begin
  inherited create;
  Init(axprovide,Purpose);
  if not assigned(x) then begin
    fxml:=axprovider.GetStructure('tstructs', pname, pcaption,purpose);
    if assigned(fxml) then begin
      transid:=axprovider.GetStructName;
      caption:=axprovider.GetStructCaption;
      SetXML(fxml);
    end;
  end else begin
    transid:=pName;caption:=pCaption;
    xml:=x;
    if vartostr(xml.DocumentElement.ChildNodes[0].Attributes['wsflds']) <> '' then
       QuickDataFlag := true;
  end;
end;

constructor TStructDef.Create(axprovide:TAxProvider; pName :String; x:IXMLDocument);
begin
  inherited create;
  Init(axprovide,'');
  transid:=pName;
  fxml := x;
  CeatedFlds := TStringlist.Create;
  CreatedDcs := TStringlist.Create;
  FldCreaedOrder := TStringlist.Create;
end;

Procedure TStructDef.Init(axprovide : TAxprovider;purpose : String);
begin
  fcount:=0;
  rPurpose :=  purpose;
  axprovider:=axprovide;
  flds := tlist.create;
  frames:=tlist.create;
  fgs := tlist.create;
  popgrids := tlist.Create;
  exprsetlist := TStringList.create;
  TrackFieldsList := TStringList.Create;
  ParentList:=TStringList.Create;
  PopParentList:=TStringList.Create;
  ImgFldList := TStringList.Create;
  HasDetStruct := False;
  ActiveFrame:=1; ActiveField:=0;
  treetables:='';treeparents:='';
  PrimaryFields := '';
  HasViewImgFlds := False;
  HasImgPath := False;
  HasImage := false;
  oldfields := TStringList.Create;
  TableList := TStringList.Create;
  oldfieldslist := '';
  stype := '';
  timezone_diff := 0;
  NoEmptyImgFlds := '';
  DynamicLabels := '';
  SearchCond := '';
  TrackAllFields := False;
  ListView := False;
  HasWorkFlow := False;
  Traceability := False;
  HasPickListFlds := False;
  sdraft := False;
  CeatedFlds := nil;
  CreatedDcs := nil;
  FldCreaedOrder := nil;
  HasAutoGenFields := False;
  quickload := false;
  QuickDataFlag := false;
  GetDependants := True;
  visibleDCs := '';
  FgAutoShowMapFlds := ',';
  tmpSXML := nil;
  priorfrmno := 0;
  ForDep := False;
  GetPList := False;
  FldLoadedDuringStructLoad := True;
  isPegAttached := false;
  IsAmendmentEnabled := false;
  axRule_Validate := nil;
  axRule_AllowEmpty := nil;
  axRule_AllowDuplicate := nil;
  axRule_ValidateOnSave := nil;
  if assigned(axprovider.dbm.gf.AxRuleNode) then SetAxRulesNodes;
  HasOutBound := false;
  HasAutoPrints := false;
  HasFormNotify := false;
  bGenmapThroughQueue := false;
end;

//SetPEGFlag
Procedure TStructDef.SetPEGandAmendFlag;
var
  pegXDS : TXDS;
  anode : IXMLNode;
begin
  try
    axprovider.dbm.gf.DoDebug.msg('SetPEGFlag starts...');
    pegXDS := nil;
    try
      anode := XML.DocumentElement;
      (*
      it may be necessary to check the axprocessdefv2 table. The PEG attribute will be added to the struct XML,
      but it belongs to the calling point(SOAP/REST). When dealing with REST API call operations,
      we may not get PEG flag,since the structure XML will not be attached with the request.

      To address this, we are directly checking the PEG attribute by querying the axprocessdefv2 table.
      *)
      //PEG
      pegXDS := axprovider.CheckAxProcessDef(transid);
      if Assigned(pegXDS) and (pegXDS.CDS.RecordCount > 0) then
      begin
        anode.Attributes['peg'] := 'true';
        isPegAttached := True;
      end
      else
      begin
        anode.Attributes['peg'] := 'false';
        isPegAttached := False;
      end;
      if Assigned(pegXDS) then
      begin
        if pegXDS.Active then pegXDS.close;
        FreeAndnil(pegXDS);
      end;
      pegXDS := nil;
      //Amendment
      pegXDS := axprovider.IsAmendmentEnabled(transid);
      if Assigned(pegXDS) and (pegXDS.CDS.RecordCount > 0) then
      begin
        anode.Attributes['amendment'] := 'true';
        IsAmendmentEnabled := True;
      end
      else
      begin
        anode.Attributes['amendment'] := 'false';
        IsAmendmentEnabled := False;
      end
    Except on E:Exception do
      axprovider.dbm.gf.DoDebug.msg('\uStructDef\SetPEGFlag - '+e.Message);
    end;
    axprovider.dbm.gf.DoDebug.msg('SetPEGFlag ends.');
  finally
    if Assigned(pegXDS) then
    begin
      if pegXDS.Active then pegXDS.close;
      FreeAndnil(pegXDS);
    end;
  end;
end;

//SetAxOtherSettings
Procedure TStructDef.SetAxOtherSettings;
var
  q : TXDS;
  anode : IXMLNode;
begin
  try
    //If the transaction set for SetAxOtherSettings.
    axprovider.dbm.gf.DoDebug.msg('SetAxOtherSettings starts...');
    q := nil;
    try
      anode := XML.DocumentElement;
      //outbound
      q := axprovider.dbm.GetXDS(nil);
      q.buffered:=true;
      q.CDS.CommandText := 'select stransid from axoutqueues Where lower(active) = ''t'' and ' + axprovider.dbm.gf.SQLLower +'(stransid) = '  +  quotedstr(lowercase(transid)) +' and lower(axqueuesource) = ''form''';
      q.open;
      if q.CDS.RecordCount > 0 then
      begin
        anode.Attributes['axoutboundqueues'] := 'true';
        HasOutBound := true;
      end
      else
      begin
        anode.Attributes['axoutboundqueues'] := 'false';
        HasOutBound := false;
      end;
      //AxAutoPrints
      q.close;
      q.CDS.CommandText := 'select stransid from AxAutoPrints Where ' + axprovider.dbm.gf.SQLLower +'(stransid) = '  +  quotedstr(lowercase(transid));
      q.open;
      if q.CDS.RecordCount > 0 then
      begin
        anode.Attributes['autoprints'] := 'true';
        HasAutoPrints := true;
      end
      else
      begin
        anode.Attributes['autoprints'] := 'false';
        HasAutoPrints := false;
      end;
      //AxAutoPrints
      q.close;
      q.CDS.CommandText := 'select stransid from AxFormNotify Where lower(active) = ''t'' and ' + axprovider.dbm.gf.SQLLower +'(stransid) = '  +  quotedstr(lowercase(transid));
      q.open;
      if q.CDS.RecordCount > 0 then
      begin
        anode.Attributes['formnotify'] := 'true';
        HasFormNotify := true;
      end
      else
      begin
        anode.Attributes['formnotify'] := 'false';
        HasFormNotify := false;
      end;
      q.close;
    Except on E:Exception do
      axprovider.dbm.gf.DoDebug.Msg('Error :' +e.Message);
    end;
    axprovider.dbm.gf.DoDebug.msg('SetAxOtherSettings ends.');
  finally
    if Assigned(q) then
    begin
      FreeAndnil(q);
    end;
  end;
end;


procedure TStructDef.SetAxRulesNodes;
   var n : ixmlnode;
begin
  n :=  axprovider.dbm.gf.AxRuleNode.ChildNodes.FindNode('validate');
  if assigned(n) then axRule_Validate := n.CloneNode(true);
  n :=  axprovider.dbm.gf.AxRuleNode.ChildNodes.FindNode('allowempty');
  if assigned(n) then axRule_AllowEmpty := n.CloneNode(true);
  n :=  axprovider.dbm.gf.AxRuleNode.ChildNodes.FindNode('allowduplicate');
  if assigned(n) then axRule_AllowDuplicate := n.CloneNode(true);
  n :=  axprovider.dbm.gf.AxRuleNode.ChildNodes.FindNode('validate_onsave');
  if assigned(n) then axRule_ValidateOnSave := n.CloneNode(true);
end;

destructor TStructDef.Destroy;
var i:integer;
begin
  try
    clearfields;
    flds.clear;
    flds.Destroy;
  except
  end;
  try
    for i:=0 to frames.count-1 do
      dispose(pfrm(frames[i]));
    frames.free;
  except
  end;
  try
    for i:=0 to fgs.count-1 do begin
      pfg(fgs[i]).Map.Free;
      pfg(fgs[i]).Caps.Free;
      if assigned(pfg(fgs[i]).q) then begin
        pfg(fgs[i]).q.Close;
        pfg(fgs[i]).q.free;
      end;
      dispose(pfg(fgs[i]));
    end;
    fgs.Free;
  except
  end;
  try
  for i:=0 to popgrids.count-1 do begin
    if assigned(pPopGrid(popgrids[i]).AutoFill) then
      pPopGrid(popgrids[i]).AutoFill.Free;
    dispose(pPopgrid(popgrids[i]));
  end;
  popgrids.free;
  except
  end;
  try
    exprsetlist.clear;
    FreeAndNil(exprsetlist);
  except
  end;
  try
    TrackFieldsList.clear;
    FreeAndNil(TrackFieldsList);
  except
  end;
  try
    ParentList.clear;
    FreeAndNil(ParentList);
  except
  end;
  try
    PopParentList.clear;
    FreeAndNil(PopParentList);
  except
  end;
  try
    ImgFldList.clear;
    FreeAndNil(ImgFldList);
  except
  end;
  try
    oldfields.Clear;
    FreeAndNil(oldfields);
  except
  end;
  try
    TableList.Clear;
    FreeAndNil(TableList);
  except
  end;
  if assigned(CeatedFlds) then
  begin
    try
      CeatedFlds.Clear;
      FreeAndNil(CeatedFlds);
    except
    end;
  end;
  if assigned(CreatedDcs) then
  begin
    try
      CreatedDcs.Clear;
      FreeAndNil(CreatedDcs);
    except
    end;
  end;
  if assigned(FldCreaedOrder) then
  begin
    try
      FldCreaedOrder.Clear;
      FreeAndNil(FldCreaedOrder);
    except
    end;
  end;
  axRule_Validate := nil;
  axRule_AllowEmpty := nil;
  axRule_AllowDuplicate := nil;
  axRule_ValidateOnSave := nil;
  axprovider.dbm.gf.AxRuleNode := nil;
  inherited;
end;

procedure TStructDef.ClearFields;
var i:integer;
begin
  for i:=0 to flds.count-1 do begin
    try
    if assigned(pfld(flds[i]).dependents) then
    begin
      pfld(flds[i]).dependents.Clear;
      pfld(flds[i]).dependents.Free;
    end except on e:Exception do
      begin
      if assigned(axprovider) then  axprovider.dbm.gf.DoDebug.Log(axprovider.dbm.gf.Axp_logstr+'\uStructDef\ClearFields - '+e.Message);
      pfld(flds[i]).dependents := nil ;
      end;
    end;
    try
    if assigned(pfld(flds[i]).deps) then
    begin
      pfld(flds[i]).deps.Clear;
      pfld(flds[i]).deps.Free;
    end except on e:Exception do
      begin
        if assigned(axprovider) then  axprovider.dbm.gf.DoDebug.Log(axprovider.dbm.gf.Axp_logstr+'\uStructDef\ClearFields - '+e.Message);
        pfld(flds[i]).deps := nil;
      end;
    end;
    try
    if assigned(pfld(flds[i]).detmap) then
    begin
      pfld(flds[i]).detmap.Clear;
      pfld(flds[i]).detmap.Free;
    end except on e:Exception do
      begin
        if assigned(axprovider) then  axprovider.dbm.gf.DoDebug.Log(axprovider.dbm.gf.Axp_logstr+'\uStructDef\ClearFields - '+e.Message);
        pfld(flds[i]).detmap := nil;
      end;
    end;
    try
    if assigned(pfld(flds[i]).listvals) then
    begin
      pfld(flds[i]).listvals.Clear;
      pfld(flds[i]).listvals.Free;
    end except on e:Exception do
      begin
        if assigned(axprovider) then  axprovider.dbm.gf.DoDebug.Log(axprovider.dbm.gf.Axp_logstr+'\uStructDef\ClearFields - '+e.Message);
        pfld(flds[i]).listvals := nil;
      end;
    end;
    try
    if assigned(pfld(flds[i]).qselect) then begin
      pfld(flds[i]).QSelect.Close;
      pfld(flds[i]).QSelect.Free;
    end;
    except on e:Exception do
      begin
        if assigned(axprovider) then  axprovider.dbm.gf.DoDebug.Log(axprovider.dbm.gf.Axp_logstr+'\uStructDef\ClearFields - '+e.Message);
        pfld(flds[i]).QSelect := nil;
      end;
    end;
    try
    if assigned(pfld(flds[i]).SQL) then
    begin
      pfld(flds[i]).SQL.Clear;
      pfld(flds[i]).SQL.Free;
    end except on e:Exception do
      begin
        if assigned(axprovider) then  axprovider.dbm.gf.DoDebug.Log(axprovider.dbm.gf.Axp_logstr+'\uStructDef\ClearFields - '+e.Message);
        pfld(flds[i]).SQL := nil;
      end;
    end;
    try
    if assigned(pfld(flds[i]).SearchSQL) then
    begin
      pfld(flds[i]).SearchSQL.Clear;
      pfld(flds[i]).SearchSQL.Free;
    end except on e:Exception do
      begin
        if assigned(axprovider) then  axprovider.dbm.gf.DoDebug.Log(axprovider.dbm.gf.Axp_logstr+'\uStructDef\ClearFields - '+e.Message);
        pfld(flds[i]).SearchSQL := nil;
      end;
    end;
    try
    if assigned(pfld(flds[i]).displaydetail) then
    begin
      pfld(flds[i]).displaydetail.Clear;
      pfld(flds[i]).displaydetail.Free;
    end except on e:Exception do
      begin
        if assigned(axprovider) then  axprovider.dbm.gf.DoDebug.Log(axprovider.dbm.gf.Axp_logstr+'\uStructDef\ClearFields - '+e.Message);
        pfld(flds[i]).displaydetail := nil;
      end;
    end;
    try
    if assigned(pfld(flds[i]).prefixflds) then
    begin
      pfld(flds[i]).prefixflds.Clear;
      pfld(flds[i]).prefixflds.Free;
    end except on e:Exception do
      begin
        if assigned(axprovider) then  axprovider.dbm.gf.DoDebug.Log(axprovider.dbm.gf.Axp_logstr+'\uStructDef\ClearFields - '+e.Message);
        pfld(flds[i]).prefixflds := nil;
      end;
    end;
    try
    if assigned(pFld(flds[i]).pickfields) then
    begin
      pFld(flds[i]).pickfields.Clear;
      pFld(flds[i]).pickfields.free;
    end except on e:Exception do
      begin
        if assigned(axprovider) then  axprovider.dbm.gf.DoDebug.Log(axprovider.dbm.gf.Axp_logstr+'\uStructDef\ClearFields - '+e.Message);
        pfld(flds[i]).pickfields := nil;
      end;
    end;
    try
    if assigned(pFld(flds[i]).pickcaptions) then
    begin
      pFld(flds[i]).pickcaptions.Clear;
      pFld(flds[i]).pickcaptions.free;
    end except on e:Exception do
      begin
        if assigned(axprovider) then  axprovider.dbm.gf.DoDebug.Log(axprovider.dbm.gf.Axp_logstr+'\uStructDef\ClearFields - '+e.Message);
        pfld(flds[i]).pickcaptions := nil;
      end;
    end;
    try
    if assigned(pFld(flds[i]).datarows) then
    begin
      pFld(flds[i]).datarows.clear;
      pFld(flds[i]).datarows.free;
    end except on e:Exception do
      begin
        if assigned(axprovider) then  axprovider.dbm.gf.DoDebug.Log(axprovider.dbm.gf.Axp_logstr+'\uStructDef\ClearFields - '+e.Message);
        pfld(flds[i]).datarows := nil;
      end;
    end;
    try
    if assigned(pFld(flds[i]).FldUX) then
    begin
      pFld(flds[i]).FldUX.free;
      pFld(flds[i]).FldUX := nil;
    end except on e:Exception do
      begin
        if assigned(axprovider) then  axprovider.dbm.gf.DoDebug.Log(axprovider.dbm.gf.Axp_logstr+'\uStructDef\ClearFields - '+e.Message);
        pFld(flds[i]).FldUX := nil;
      end;
    end;
    try
      dispose(pFld(flds[i]));
    except on e:Exception do
      if assigned(axprovider) then  axprovider.dbm.gf.DoDebug.Log(axprovider.dbm.gf.Axp_logstr+'\uStructDef\ClearFields - '+e.Message);
    end;
  end;

end;

Function SortFldsLoad(Item1, Item2: Pointer): Integer;
Begin
  result := 0;
  If pFld(Item1).Frameno < pFld(Item2).Frameno Then
    result := -1
  Else If pFld(Item1).FrameNo > pFld(Item2).Frameno Then
    result := 1
  Else Begin
    If pFld(Item1).TableName < pFld(Item2).Tablename Then result := -1
    Else If pFld(Item1).TableName > pFld(Item2).Tablename Then
      result := 1
    Else If pFld(Item1).TableName = pFld(Item2).Tablename Then Begin
      If pFld(Item1).OrderNo < pFld(Item2).OrderNo Then result := -1
      Else If pFld(Item1).OrderNo > pFld(Item2).OrderNo Then result := 1
      Else If pFld(Item1).OrderNo = pFld(Item2).OrderNo Then result := 0;
    End;
  End;
End;

Function SortFldsDefault(Item1, Item2: Pointer): Integer;
Begin
  result := 0;
  If pFld(Item1).Frameno < pFld(Item2).Frameno Then
    result := -1
  Else If pFld(Item1).FrameNo > pFld(Item2).Frameno Then
    result := 1
  Else Begin
    If pFld(Item1).OrderNo < pFld(Item2).OrderNo Then result := -1
    Else If pFld(Item1).OrderNo > pFld(Item2).OrderNo Then result := 1
    Else If pFld(Item1).OrderNo = pFld(Item2).OrderNo Then result := 0;
  End;
End;

function TStructDef.GetField(fieldname: String): pfld;
  var i : integer;
      n : ixmlnode;
begin
  result:=nil;
  if (quickload) then
  begin
    i := CeatedFlds.IndexOf(lowercase(fieldname));
    if i > -1 then result:=pfld(flds[i])
    else begin
      n := XML.DocumentElement.ChildNodes.FindNode(fieldname);
      if not assigned(n) then n := XML.DocumentElement.ChildNodes.FindNode(lowercase(fieldname));
      if not assigned(n) then
      begin
         if not assigned(tmpSXML) then tmpSXML := LoadXMLData(lowercase(XML.DocumentElement.XML));
         if assigned(tmpSXML) then i := tmpSXML.DocumentElement.ChildNodes.IndexOf(lowercase(fieldname));
         if i > -1 then n := XML.DocumentElement.ChildNodes.Get(i);
      end;
      if assigned(n) then
      begin
        GetFrameForWebLoad(vartostr(n.Attributes['dcno']));
        xload_selected_field(n);
        result := fld;
      end;
    end;
  end else
  begin
    fieldname:=lowercase(fieldname);
    for i:=0 to flds.Count-1 do begin
      if fieldname=lowercase(pfld(flds[i]).FieldName) then begin
        result:=pfld(flds[i]);
        break;
      end;
    end;
  end;
end;

function TStructDef.GetFieldIndex(fieldname: String): integer;
var i:integer;
begin
  result:=-1;
  fieldname:=lowercase(fieldname);
  for i:=0 to flds.Count-1 do begin
    if fieldname=lowercase(pfld(flds[i]).FieldName) then begin
      result:=i;
      break;
    end;
  end;
end;

function TStructDef.SearchDisplayName(s:String):pfld;
var i:integer;
begin
  result:=nil;
  s:=lowercase(s);
  for i:=0 to flds.count-1 do begin
    if s=lowercase(pfld(flds[i]).dispname) then begin
      result:=pfld(flds[i]);
      break;
    end;
  end;
end;

procedure TStructDef.loaddef(pName, pCaption:String);
begin
  fxml:=axprovider.GetStructure('tstructs', pname, pcaption,rPurpose);
  transid:=axprovider.GetStructName;
  caption:=axprovider.GetStructCaption;
  SetXML(fXML);
end;

procedure TStructDef.SetXML(const Value: IXMLDocument);
var i:integer;
    cat : String;
    n:ixmlnode;
begin
  fXML := Value;
  framenames:='';
  gridstring:='';
  tables:='';
  MastFrameNos:='';
  tree:=vartostr(xml.documentelement.attributes['tree']);
  primaryfield := vartostr(xml.documentelement.attributes['primaryfield']);
  xloadstruct(xml.documentelement.ChildNodes[0]);
  for i:=1 to xml.DocumentElement.ChildNodes.Count-1 do begin
    n:=xml.DocumentElement.ChildNodes[i];
    if n.NodeName='iframes' then continue;
    cat:=vartostr(n.attributes['cat']);
    if cat='dc' then
      xloaddc(n)
    else if cat='field' then
      xloadfield(n)
    else if cat='fillgrid' then
      xloadfillgrid(n)
    else if cat='genmap' then
      xloadgenmap(n)
    else if cat='mdmap' then
      xloadmdmap(n);
  end;
  Delete(PrimaryFields,1,1);
  GetTableList;
  UpdateHasGridParents;
  //OrderDeps;
  if oldfieldslist <> '' then CreateOldFiledsList;

  if framenames<>'' then delete(framenames, 1, 1);
  if MastFrameNos<>'' then delete(MastFrameNos, 1, 1);
end;

procedure TStructDef.xloadstruct(x: ixmlnode);
var i:integer;
    n,cnode:ixmlnode;
    s,uname,grpname:String;
    snode : ixmlnode;
begin
//  nochange:=vartostr(x.ChildValues[xml_nochange]);
  docdatefield:=vartostr(x.ChildValues[xml_datefield]);
//  track:=x.ChildValues[xml_track]='True';
  track := False;
  attach := false;
  attach := x.ChildValues[xml_attach]='True';
  HasWorkFlow := x.ChildValues[xml_WorkFlow]='True';
  sdraft := Vartostr(x.ChildValues[xml_SaveDraft])='True';
  n := x.ChildNodes[xml_track];
  track_chng := lowercase(vartostr(n.Attributes['tchng']))='y';    //ch1
  uname := lowercase(axprovider.dbm.gf.username);
  grpname := lowercase(axprovider.dbm.gf.usergroup);
  if n.HasAttribute('track') then
  begin
    if (vartostr(n.Attributes['track']) = 't') then
    begin
      if (vartostr(n.Attributes['ausers']) = 't') then
        track := True
      else begin
        cnode := n.ChildNodes.FindNode(xml_TrackUsers);
        if (cnode <> nil) then
        begin
          for i := 0 to cnode.ChildNodes.Count - 1 do
          begin
            if (lowercase(vartostr(cnode.ChildNodes[i].NodeValue)) = uname) or
              (lowercase(vartostr(cnode.ChildNodes[i].NodeValue)) = grpname) then
            begin
              track := True;
              break;
            end;
          end;
        end;
      end;
    end;
  end;
  if (track) then
  begin
    if (vartostr(n.Attributes['aflds']) = 't') then
      TrackAllFields := True
    else begin
      cnode := n.ChildNodes.FindNode(xml_TrackFields);
      TrackAllFields := False;
      if (cnode <> nil) then
      begin
        for i := 0 to cnode.ChildNodes.Count - 1 do
          trackFieldsList.Add(vartostr(cnode.ChildNodes[i].NodeValue));
      end;
    end;
  end;
  delcontrol:=vartostr(x.ChildValues[xml_delcontrol]);
  savecontrol:=vartostr(x.ChildValues[xml_savecontrol]);
  transid := vartostr(x.ChildValues[xml_name]);
  caption := vartostr(x.ChildValues[xml_caption]);
  treeparents := vartostr(x.ChildValues[xml_treeparents]);
  treetables := Transid+treeparents;
  SchemaName := Trim(vartostr(x.ChildValues[xml_Schema]));
  snode := x.ChildNodes[xml_searchcondition];
  if (snode <> nil) then
  begin
    snode := snode.ChildNodes['l'];
    if snode <> nil then
     SearchCond := Trim(vartostr(snode.ChildValues['l']));
  end else SearchCond := '';
  ListView := vartostr(x.ChildValues[xml_ListView]) = 'True';
  WorkFlow := vartostr(x.ChildValues[xml_WorkFlow]) = 'True';
  Traceability := vartostr(x.ChildValues[xml_Traceability]) = 'True';
  snode := nil;
  if assigned(xml) then
  begin
    (*
    Checking if attributes exist and then assigning values based on the config
    doesn't work, because this logic is executed even when saving the structure
    itself. At that point, attributes are added to the XML with default 'false'
    values. So, if any changes are later made in the config, they won't be
    reflected in the XML. A flag is already added to identify if the XML is
    received from the client (which will have recent config values).
    If the XML is not received from the client,
    it will be read from the database, and
    settings will be assigned based on
    the config table (asattribute check removed). This is necessary because relying on the table's XML won't
    work if there have been recent changes in the config.


    isSXMLGetFromClient should be set to false when the call is made from WriteDefinition,
    as the settings should get updated if there are any changes.
    Therefore, it is set to false in WriteStrictDef.

    *)
    if (not axprovider.dbm.gf.isSXMLGetFromClient)
       {and ((not xml.documentelement.HasAttribute('peg'))
       or (not xml.documentelement.HasAttribute('amendment')))} then
      SetPEGandAmendFlag
    else
    begin
      if vartostr(xml.documentelement.attributes['peg'])= 'true' then isPegAttached := true;
      if vartostr(xml.documentelement.attributes['amendment'])= 'true' then IsAmendmentEnabled := true;
    end;
    if (not axprovider.dbm.gf.isSXMLGetFromClient)
       {and ((not xml.documentelement.HasAttribute('axoutboundqueues'))
       or (not xml.documentelement.HasAttribute('autoprints'))
       or (not xml.documentelement.HasAttribute('formnotify')))} then
    begin
      SetAxOtherSettings;
    end
    else
    begin
      if vartostr(xml.documentelement.attributes['axoutboundqueues'])= 'true' then HasOutBound := true;
      if vartostr(xml.documentelement.attributes['autoprints'])= 'true' then HasAutoPrints := true;
      if vartostr(xml.documentelement.attributes['formnotify'])= 'true' then HasFormNotify := true;
    end;
  end;
{  n:=x.ChildNodes[xml_exprset];
  for i:=0 to n.ChildNodes.Count-1 do begin
    s:=trim(vartostr(n.ChildNodes[i].NodeValue));
    if (s<>'') and (s[1]='{') then s:=lowercase(s);
    exprsetlist.Add(s);
  end;
}
end;

procedure TStructDef.xloaddc(x: ixmlnode);
var s:String;
begin
  new(frm);
  frm.FrameNo:=x.ChildValues[xml_frameno];
  frm.Parent:=0;
  frm.PageNo:=-1;
  frm.PopParent:=-1;
  frm.jsonstr:='';
  frm.mustloadsql := '';
  frm.TableName:=vartostr(x.ChildValues[xml_table]);
  s:=vartostr(x.childvalues[xml_asgrid]);
  gridstring:=gridstring+lowercase(s[1]);
  frm.AsGrid:=s='True';
  s:=vartostr(x.ChildNodes[xml_Popup].Attributes['pop']);
  frm.Popup:=s='t';
  s:=vartostr(x.ChildValues[xml_dcAllowEmpty]);
  frm.AllowEmpty:=s='True';
  // added for AllowChange
  s:=vartostr(x.ChildValues[xml_AllowChange]);
  frm.AllowChange:=s='True';

  s:=vartostr(x.ChildValues[xml_AddDcRows]);
  if trim(s) ='' then frm.AllowAddRow := true
  else frm.AllowAddRow:=s='True';

  s:=vartostr(x.ChildValues[xml_DeleteDcRows]);
  if trim(s) ='' then frm.AllowDeleteRow := true
  else frm.AllowDeleteRow:=s='True';

  frm.ReadOnly := True;
  framecount:=framecount+1;
  framenames:=framenames+','+x.NodeName;
  if SchemaName = '' then
    tables:=tables+','+frm.TableName
  else
    tables:=tables+','+SchemaName+'.'+frm.TableName;
  if frm.FrameNo=1 then
    primarytable:=frm.TableName;
  frm.caption:=vartostr(x.ChildValues[xml_caption]);
  frm.comp:=nil;
  frm.ActRowDeps := vartostr(x.Attributes['rdf']);
  frm.PopIndex := -1;
  if frm.Popup then
    frm.PopIndex := CreatePopGrid(x);
  frm.StartIndex:=-1;
  frm.FieldCount:=0;
  frm.Rowcount := 0;
  frm.HasDataRows := false;
  frames.Add(frm);
  fieldno:=1;
end;

procedure TStructDef.xloadfield(x: ixmlnode);
var dnode, lnode , cnode, pfxnode : ixmlnode;
    i:integer;
    pchar,s : String;
begin
  if axprovider.dbm.gf.IsService then x.Attributes['dcno'] := inttostr(frm.FrameNo);
  new(fld);
  fld.pickfields:=nil;
  fld.pickcaptions:=nil;
  fld.PickListMode := plmNone;
  fld.PickListDef := '';
  fld.PickRecFound := False;
  fld.PickListLevel := 0;
  fld.WordSearch := False;
  fld.UsedQuotedStr := False;
  fld.DataRows:=TStringList.create;
  fld.HasHyperLink := False;
  fld.FieldName := x.ChildValues[xml_name];
  if (x.ChildValues[xml_save]='True')   then fld.Tablename:=frm.TableName  //and (lowercase(x.ChildValues[xml_datatype])<>'image')
  else fld.Tablename:='';
  fld.SaveValue := x.ChildValues[xml_save]='True';
  if fld.SaveValue then
  begin
     if x.HasAttribute('encrypted') then
       fld.EncryptValue := vartostr(x.attributes['encrypted'])='T'
     else fld.EncryptValue := false;
  end else fld.EncryptValue := false;
  fld.DataType := lowercase(x.ChildValues[xml_datatype]);
  fld.DataType := fld.DataType[1];
  if fld.DataType = 'n' then
  begin
     if x.HasAttribute('customdecimal') then
       fld.CustomDecimal := vartostr(x.attributes['customdecimal'])='T'
     else fld.CustomDecimal := false;
  end;
  fld.Width := x.ChildValues[xml_datawidth];
  fld.Dec := strtoint(vartostr(x.ChildValues[xml_dec]));
  fld.CurDec := fld.Dec;
  if (fld.DataType = 'n') and (fld.CustomDecimal) then
  begin
     if axprovider.dbm.gf.axdecimal <> -1 then
        fld.Dec := axprovider.dbm.gf.axdecimal;
  end;
  fld.DBDec := 0;
  if x.HasAttribute('dbdecimal') then
  begin
    fld.DBDec := strtoint(vartostr(x.attributes['dbdecimal']));
    if fld.DBDec < fld.Dec then
      fld.DBDec := fld.Dec;
  end;
  fld.Caption := vartostr(x.ChildValues[xml_caption]);
  if pos('{',fld.Caption) > 0 then AddToDynamicLabel('lbl'+fld.FieldName);
  fld.FrameNo := frm.FrameNo;
  fld.FldUX := TStringlist.Create;
  if x.HasAttribute('fieldtype') then
     fld.CustType := vartostr(x.attributes['fieldtype']);
  fld.Empty:=x.ChildValues[xml_empty]='True';
  if (not fld.Empty) and (fld.Caption <> '') then
     fld.Caption := fld.Caption+'*';
  if (fld.DataType = 'i') then
  begin
    ImgFldList.Add(fld.FieldName+'=i') ;
    if not fld.Empty then begin
      if NoEmptyImgFlds = '' then
        NoEmptyImgFlds := fld.FieldName
      else
        NoEmptyImgFlds := NoEmptyImgFlds+','+fld.FieldName;
    end;
    HasImage := true;
  end else if lowercase(fld.FieldName) = 'dc'+Trim(Inttostr(fld.FrameNo))+'_image' then
  begin
    ImgFldList.Add(fld.FieldName+'=c');
    HasViewImgFlds := True;
  end;
  if lowercase(fld.FieldName) = 'dc'+Trim(Inttostr(fld.FrameNo))+'_imagepath' then
    HasImgPath := True;
  fld.NoDuplicate := not(x.ChildValues[xml_duplicate]='True');
  if fld.NoDuplicate then
    PrimaryFields := primaryFields+','+fld.FieldName;
  fld.AsGrid := frm.AsGrid;
  fld.ValExprn := -1;
  try
    fld.cvalexp:=vartostr(x.ChildValues[xml_vexp]);
  except on e:Exception do
    begin
      if assigned(axprovider) then  axprovider.dbm.gf.DoDebug.Log(axprovider.dbm.gf.Axp_logstr+'\uStructDef\xloadfield - '+e.Message);
      fld.cvalexp:=vartostr(x.ChildNodes.FindNode(xml_vexp).Attributes['expr']); // added because of requirement in MyDocs
    end;
  end;
  fld.orderno := fieldno;
  inc(fieldno);
  fld.hidden := x.ChildValues[xml_hide]='True';
  fld.readonly := x.ChildValues[xml_readonly]='True';
  if frm.ReadOnly then
    frm.ReadOnly := fld.readonly;
  if vartostr(x.ChildValues[xml_Tabstop])<>'' then begin               // focus the read only field when the Tabstop is true
    if lowercase(vartostr(x.ChildValues[xml_Tabstop]))='true' then
      fld.Tabstop:=true
    else if lowercase(vartostr(x.ChildValues[xml_Tabstop]))='false' then
      fld.Tabstop:=false;
  end
  else
    fld.Tabstop:=not fld.readonly;
  fld.AllowChange := frm.AllowChange;
  fld.SetCarry := x.childvalues[xml_setcarry]='True';
  fld.ApplyComma := x.ChildValues[xml_applycomma] = 'True';
  fld.OnlyPositive := x.ChildValues[xml_onlypositive] = 'True';
  fld.searchcol:=1;
  if fld.Caption='' then fld.DispName:=fld.FieldName else fld.DispName:=fld.Caption;
  fld.DisplayTotal := x.ChildValues['disptot']='True';
  fld.ClientValidation := x.ChildValues[xml_cValidate]='True';
  fld.listvals:=nil;
  fld.deps:=nil;
  fld.detmap:=nil;
  fld.LinkField:='';
  fld.SourceField:='';
  fld.Sourcetable:='';
  fld.SourceTransid:='';
  fld.Suggestive:=false;
  fld.multiline:=false;
  fld.FromList:=false;
  fld.Refresh:=false;
  fld.Autoselect := false;
  fld.HasParams := false;
  fld.DynamicParams := false;
  fld.DetTransid:='';
  fld.DetCondition:='';
  fld.PopIndex := -1;
  fld.DispField := False;
  fld.IsBtnField := False;
  fld.IsParentField := False;
  fld.Mask:=vartostr(x.ChildValues[xml_mask]);
  fld.HasGridParents:=false;
  fld.Dependents:=nil;
  fld.DependentTypes:='';
  fld.RefreshOnChange := false;
  fld.cl_multiselect := false;
  fld.RefOnChngFlds := '';
  getdeps(fld, x);
  getdetmap(fld, x);
  s := vartostr(x.childvalues[xml_pattern]);
  if (lowercase(s) = 'isalpha') or (lowercase(s) = 'isnumeric') or (lowercase(s) = 'isemail')
      or (lowercase(s) = 'isalphanumeric') or (lowercase(s) = 'isphone') or (lowercase(s) = 'isurl') then
    fld.Pattern:= ''
  else
    fld.Pattern:= s;
  fld.Hint := vartostr(x.ChildValues[xml_Hint]);
  pchar := vartostr(x.ChildValues[xml_pwordchar]);
  if pchar = '' then
    fld.pwchar := #0
  else
    fld.pwchar := AnsiChar(pchar[1]);
  fld.ModeofEntry := lowercase(x.ChildValues[xml_moe]);
  fld.SourceKey := x.ChildValues[xml_sourcekey]='True';
  convertmoe;
  fld.Exprn := -1;
  fld.cexp := vartostr(x.ChildValues[xml_expr]);

  dnode:=x.ChildNodes.FindNode(xml_details);
  fld.SequenceNode := nil;
  fld.prefixflds := nil;
  if lowercase(fld.ModeofEntry)='autogenerate' then
  begin
    HasAutoGenFields := true;
    fld.SequenceNode := dnode.ChildNodes.FindNode(xml_Sequence);
    fld.prefixflds := TStringList.Create;
    for i := 0 to fld.SequenceNode.ChildNodes.Count - 1 do
    begin
      pfxNode := fld.SequenceNode.ChildNodes[i];
      s := Trim(vartostr(pfxNode.ChildValues[xml_PrefixField]));
      if s <> '' then
        fld.prefixflds.add(s);
    end;
  end;
  if assigned(dnode.ChildNodes.FindNode(xml_sgwidth)) then
    fld.sgwidth:= VarToStr(dnode.ChildValues[xml_sgwidth]);
  if vartostr(dnode.ChildValues[xml_sgheight]) <> '' then
    fld.sgheight:= StrToInt(vartostr(dnode.ChildValues[xml_sgheight]));

  fld.cfield := vartostr(dnode.ChildValues[xml_cfield]);
  fld.QSelect:=nil;
  sType := '';
  if fld.ModeofEntry='select' then begin
    fld.SourceField:=vartostr(dnode.ChildValues[xml_source]);
    fld.SourceTransid:=vartostr(dnode.ChildNodes[xml_source].Attributes['cap']);
    if (fld.AsGrid) and (fld.SourceField <> '') and (fld.SourceTransid <> '')   then
      fld.HasHyperLink := True;

    stype := lowercase(vartostr(dnode.ChildNodes[xml_source].Attributes['stype']));
    if stype <> '' then
       stype := ifthen(stype='sql','s','t');
    if fld.SourceKey then begin
      fld.Sourcetable:=dnode.ChildNodes[xml_source].Attributes['table'];
    end else begin
      if (dnode.ChildNodes[xml_source].HasAttribute('scol')) and
        ((vartostr(dnode.ChildNodes[xml_source].Attributes['scol']) <> ''))  then
        fld.searchcol := dnode.ChildNodes[xml_source].Attributes['scol'];
    end;
    lnode:=dnode.ChildNodes.FindNode(xml_list);
    if (assigned(lnode)) and (lnode.ChildNodes.Count>0)  then begin
      stype := 'l';
      fld.QSelect:=axprovider.dbm.GetXDS(nil);
      fld.QSelect.buffered:=true;
      if (not fileexists(axprovider.dbm.gf.startpath+'Structures\'+axprovider.dbm.gf.AppName+'\'+transid+'_'+fld.FieldName+'.cds')) or (axprovider.dbm.gf.isservice) then begin
        fld.QSelect.CDS.CommandText:= axprovider.dbm.gf.nullcds;
        fld.QSelect.Open;
        if fld.QSelect.CDS.RecordCount > 0 then
          fld.QSelect.CDS.Delete;
        for i:=0 to lnode.ChildNodes.count-1 do
          fld.QSelect.CDS.AppendRecord([lnode.ChildNodes[i].NodeValue]);
        if not axprovider.dbm.gf.isservice then
          fld.QSelect.CDS.SaveToFile(axprovider.dbm.gf.startpath+'Structures\'+axprovider.dbm.gf.AppName+'\'+transid+'_'+fld.FieldName+'.cds');
      end else begin
        fld.QSelect.CDS.LoadFromFile(axprovider.dbm.gf.startpath+'Structures\'+axprovider.dbm.gf.AppName+'\'+transid+'_'+fld.FieldName+'.cds');
      end;
      fld.searchcol:=0;
      fld.FromList:=true;
      fld.Autoselect:=dnode.ChildValues[xml_autoselect]='True'; //ch1
    end;
  end else if fld.ModeofEntry='fill' then begin
    fld.LinkField := vartostr(dnode.ChildValues[xml_fparent]);
    fld.sourcetable := vartostr(dnode.Childvalues[xml_sourcetable]);
    fld.SourceField := vartostr(dnode.ChildValues[xml_fsource]);
    fld.Suggestive := dnode.childvalues[xml_suggestive]='True';
  end else if fld.ModeofEntry='accept' then begin
    fld.Suggestive := dnode.childvalues[xml_suggestive]='True';
  end;

  cnode := x.childnodes.FindNode(xml_gui);
  fld.Kind := '';  // 9.5 1
  if assigned(cnode) then begin
    if cnode.HasAttribute('ctype') then begin
      if (vartostr(cnode.Attributes['ctype']) = 'Check list') then
      begin
        fld.ComponentType:='cl';
        fld.cl_multiselect := true;
      end else if (vartostr(cnode.Attributes['ctype']) = 'Check box') then fld.ComponentType:='cb'
      else if (vartostr(cnode.Attributes['ctype']) = 'Radio group') then begin
        fld.ComponentType:='rg';
        fld.Kind := VarToStr(cnode.Attributes['type']);  // 9.5 1
      end
      else if (vartostr(cnode.Attributes['ctype']) = 'Radio button') then fld.ComponentType:='rb'
      else fld.ComponentType:='';
      fld.Separator := ',';
      if vartostr(cnode.Attributes['ctype']) = 'Check list' then
      begin
        if VarTostr(cnode.Attributes['sep']) <> '' then
           fld.Separator := VarTostr(cnode.Attributes['sep']);
        fld.CommaSelection := True;
      end else begin
        fld.CommaSelection := False;
      end;
    end else fld.CommaSelection := False;
    fld.FontStr := vartostr(cnode.Attributes['font']);
    if fld.FontStr = ',,,' then fld.FontStr := '';
    fld.ColorStr := vartostr(cnode.Attributes['color']);
  end else begin
    fld.CommaSelection := False;
    fld.FontStr := '';
    fld.ColorStr := '';
  end;
  //  fld.sql:=vartostr(dnode.ChildValues[xml_sql]);
  fld.txtSelection := false;
  lnode:=dnode.ChildNodes.FindNode(xml_sql);
  fld.sql := nil;
  if assigned(lnode) then
  begin
    if vartostr(lnode.Attributes['mulselect']) = 'true' then fld.cl_multiselect := true;
    fld.Separator := vartostr(lnode.Attributes['mulsel']);
    if fld.Separator = '' then fld.Separator := ',';
    if (not lnode.HasChildNodes) then begin
      if Trim(vartostr(lnode.NodeValue)) <> '' then begin
        fld.sql := TStringList.Create;
        fld.sql.Add(vartostr(lnode.NodeValue));
      end;
    end else begin
  //    fld.SQL := '';
      fld.sql := TStringList.Create;
      for i := 0 to lnode.ChildNodes.Count-1 do
        fld.SQL.Add(vartostr(lnode.ChildNodes[i].NodeValue));
  //      fld.SQL := fld.SQL+vartostr(lnode.ChildNodes[i].NodeValue);
    end;
  end;
  if (assigned(fld.sql)) and  (Trim(fld.sql.Text)<>'') then begin
    fld.QSelect:=axprovider.dbm.GetXDS(nil);
    fld.QSelect.buffered:=true;
    fld.QSelect.CDS.CommandText:=fld.SQL.Text;
    if (pos('{', fld.sql.Text) > 0) or (pos('(:', fld.sql.Text) > 0) then begin
      fld.HasParams := true;
      fld.DynamicParams:=true;
    end else begin
      fld.HasParams:=fld.QSelect.cds.Params.Count > 0;
      fld.DynamicParams:=false;
    end;
    fld.Autoselect:=dnode.ChildValues[xml_autoselect]='True';
    fld.Refresh:=dnode.ChildValues[xml_refresh]='True';

    if (axprovider.dbm.gf.IsService) and (fld.ComponentType<>'') then // in getstructure service, its changing to fromlist for below condition, so its reversing here.
      fld.FromList:=false;

    fld.txtSelection:= (lnode.HasAttribute('txt')) and (lnode.Attributes['txt'] = 't');
    if not fld.txtSelection then
    begin
      if pos('{dynamicfilter',lowercase(fld.SQL.Text)) > 0 then
      fld.txtSelection := true;
    end;
    if fld.txtSelection then begin
      HasPickListFlds := True;
      stype := 'p';
      GetPickLists(fld);
    end
    else if (axprovider.dbm.gf.FastDataFlag) and (fld.ModeofEntry = 'select') then
      GetDropDownPickLists(fld);

  end;
  fld.SelType := stype;
  lnode:=x.ChildNodes.FindNode(xml_searchsql);
  fld.searchsql := nil;
  if assigned(lnode) then begin
    if lnode.ChildNodes.Count > 0 then begin
      fld.searchsql := TStringList.Create;
      for i := 0 to lnode.ChildNodes.Count-1 do
        fld.searchsql.Add(vartostr(lnode.ChildNodes[i].NodeValue));
    end;
  end;
  lnode:=x.ChildNodes.FindNode(xml_displaydetail);
  fld.displaydetail := nil;
  if assigned(lnode) then begin
    if lnode.ChildNodes.Count > 0 then begin
      fld.displaydetail := TStringList.Create;
      for i := 0 to lnode.ChildNodes.Count-1 do
        fld.displaydetail.Add(vartostr(lnode.ChildNodes[i].NodeValue));
      GetDispHyperDet(fld);
    end;
  end;
  //---Set AxRules Properties
  fld.axRule_ValExprn := -1;
  fld.axRule_ValOnSave_ValExprn := -1;
  if assigned(axprovider.dbm.gf.AxRuleNode) then SetAxRulesProps(fld.FieldName);
  //----
  if frm.startindex=-1 then frm.StartIndex:=fcount;
  inc(frm.FieldCount);
  if lowercase(copy(fld.FieldName, 1, 3)) = 'old' then  oldfieldslist := oldfieldslist + fld.FieldName + ',';
  flds.Add(fld);
  if (not (fld.readonly)) and (not (fld.hidden)) and (fld.DataType<>'i') then
  begin
    LastFieldno := flds.Count;
    frm.LastFieldNo := flds.count;
  end;
  inc(fcount);
end;

procedure TStructDef.xloadfillgrid(x: ixmlnode);
var n:ixmlnode;
    i:integer;
    s:String;
    sl : TStringList;
begin
  sl := nil;
  if x = nil then exit;
  new(fg);
  fg.fname := vartostr(x.NodeName);
  fg.name:=vartostr(x.ChildValues[xml_fgcaption]);   //ch1
  n:=x.ChildNodes[xml_fgsql];
  if n.HasChildNodes then begin
    sl := TStringList.Create;
    for i := 0 to n.ChildNodes.Count-1 do
      sl.Add(vartostr(n.ChildNodes[i].NodeValue));
  end else begin
    if Trim(vartostr(n.NodeValue)) <> '' then begin
      sl := TStringList.Create;
      sl.Add(vartostr(n.NodeValue));
    end;
  end;
//  if vartostr(x.childvalues[xml_fgsql])<>'' then begin
  if (assigned(sl)) and (Trim(sl.Text) <> '') then begin
    fg.q:=axprovider.dbm.GetXDS(nil);
    fg.q.buffered:=true;
    fg.q.CDS.CommandText:=sl.Text;
    if (pos('{', sl.Text) > 0) or (pos('(:', sl.Text) > 0) then begin
      fg.SQLText:=fg.q.CDS.CommandText;
      fg.HasParams := true;
      fg.DynamicParams:=true;
    end else begin
      fg.sqltext := '';
      fg.HasParams:=fg.q.cds.Params.Count > 0;
      fg.DynamicParams:=false;
    end;
    fg.paramnames:=vartostr(n.Attributes['plist']);
  end else fg.q:=nil;
  if assigned(sl) then
  begin
    sl.Clear;
    FreeAndNil(sl);
  end;
  fg.FromIView:=vartostr(x.ChildValues[xml_iview])='True';
  fg.MultiSelect:=vartostr(x.ChildValues[xml_multi])='True';
  fg.AutoShow:=vartostr(x.ChildValues[xml_autoshow])='True';
  fg.ValidateRow:=vartostr(x.ChildValues[xml_fgvalidate])='True';
  fg.ExecuteOnSave:=vartostr(x.ChildValues[xml_exeonsave])='True';
  fg.firmbind:=vartostr(x.ChildValues[xml_FirmBind])='True';
  fg.SelectAllRows := vartostr(x.ChildValues[xml_SelectAllRows])='True';
  s:=vartostr(x.ChildValues[xml_fgtarget]);delete(s,1,2);
  fg.TargetFrame:=axprovider.dbm.gf.strtointz(s);
  s:=vartostr(x.ChildValues[xml_sourcedc]);delete(s,1,2);
  fg.SourceFrame:=axprovider.dbm.gf.strtointz(s);
  fg.SelectOn := vartostr(x.ChildValues[xml_SelectOn]);
  s := Trim(vartostr(x.ChildValues[xml_AddRows]));
  if s = '' then
    fg.AddRows := 1
  else begin
    if s = 'Add only when grid is empty' then
      fg.AddRows := 1
    else if s = 'Initialize grid and add' then
      fg.AddRows := 2
    else if (s = 'Append rows to grid') or (s = 'Append selected rows to grid') then
      fg.AddRows := 3
    else
      fg.AddRows := 1;
  end;
  s := vartostr(x.ChildValues[xml_FooterStr]);
  if Trim(s) <> '' then
    fg.FooterStr := s;
  fg.VExp := Trim(vartostr(x.ChildValues[xml_FGVExp]));
  n:=x.ChildNodes[xml_fmap];
  fg.colwidths:='';
  fg.Map:=tstringlist.create;
  fg.Caps:=tstringlist.create;
  for i:=0 to n.ChildNodes.Count-1 do begin
    s:=n.childnodes[i].NodeName+'='+n.ChildNodes[i].Text;
    fg.map.Add(s);
    if n.HasAttribute('width') then fg.colwidths:=fg.colwidths+n.attributes['width']+',';
    if n.ChildNodes[i].HasAttribute('cap') then fg.Caps.Add(n.childnodes[i].Text+'='+VarToStr(n.ChildNodes[i].Attributes['cap']));
  end;
  fg.Groupfield := '';
  if n.HasAttribute('gfld') then
     fg.Groupfield := vartostr(n.Attributes['gfld']);
  fgs.Add(fg);
end;

procedure TStructDef.xloadgenmap(x: ixmlnode);
begin

end;

procedure TStructDef.xloadmdmap(x: ixmlnode);
begin

end;

function TStructDef.GetFrame(framename:String):pfrm;
var i,fno:integer;
begin
  result:=nil;
  delete(framename,1,2);fno:=strtoint(framename);
  for i:=0 to frames.Count-1 do begin
    if pfrm(frames[i]).FrameNo=fno then begin
      result:=pfrm(frames[i]);
      break;
    end;
  end;
end;

function TStructDef.GetFrame(frameno:integer):pfrm;
var i:integer;
begin
  result:=nil;
  for i:=0 to frames.Count-1 do begin
    if pfrm(frames[i]).FrameNo=frameno then begin
      result:=pfrm(frames[i]);
      break;
    end;
  end;
end;

procedure TStructDef.ConvertMOE;
begin
  if fld.modeofentry='to be entered' then fld.modeofentry:='accept'
  else if fld.modeofentry='to be calculated' then fld.modeofentry:='calculate'
  else if fld.modeofentry='from list' then fld.modeofentry:='select'
  else if fld.sourcekey then fld.modeofentry:='select'
  else if (copy(fld.ModeofEntry,1,5)='from ') and (not fld.SourceKey) then
    fld.ModeofEntry:='fill';
end;

function TStructDef.GetFirstFieldIndex(Frameno:Integer): integer;
var i:integer;
begin
  result:=-1;
  for i:=0 to flds.Count-1 do begin
    if FrameNo=pfld(flds[i]).FrameNo then begin
      result:=i;
      break;
    end;
  end;
end;

procedure TStructDef.SetLastEnabledFieldNo;
var i : integer;
begin
  for i := flds.Count - 1 downto 0 do begin
    if (pfld(flds[i]).hidden) or (pfld(flds[i]).readonly) or (pfld(flds[i]).DataType='i') then continue;
    LastFieldNo := i;
    exit;
  end;
end;

procedure TStructDef.GetDeps(fd:pFld;x:ixmlnode);
  var i,j,k : integer;
      n,dnode,fnode : ixmlnode;
      dlist,s,depfrm,dfld,fldtype : String;
begin
  if GetDependants then
  begin
    dnode := x.ChildNodes.FindNode('a58');
    if dnode <> nil then
    begin
      if dnode.IsTextElement then
      begin
        fd.deps:=TStringList.create;
        dlist := VarToStr(dnode.NodeValue);
        i := 1;
        s := axprovider.dbm.gf.GetnthString(dlist,i);
        fld.dependents:=TStringList.create;
        while s <> '' do
        begin
          if ForDep then
          begin
             fldtype := copy(s,1,1);
             dfld := trim(copy(s, 2, 100));
             k := fld.Dependents.IndexOf(dfld);
             if k = -1  then
             begin
               fld.DependentTypes:=fld.DependentTypes+fldtype;
               fld.Dependents.add(dfld);
             end else
             begin
               if (fld.DependentTypes[k+1] = 'e') and (fldtype = 's') then
               begin
                 Delete(fld.DependentTypes,k+1,1);
                 Insert(fldtype,fld.DependentTypes,k+1);
                 fldtype := 'g';
               end;
             end;
             if fldtype = 'd' then
               depfrm := ','+dfld
             else if fldtype <> 'g' then
               AddDeps(fd,Trim(Copy(s,2,Length(s))),i);
          end
          else begin
            fld.DependentTypes:=fld.DependentTypes+copy(s,1,1);
            fld.Dependents.add(copy(s, 2, 100));
            if copy(s,1,1) = 'd' then
              depfrm := ','+Trim(copy(s,2,3))
            else if copy(s,1,1) <> 'g' then
              AddDeps(fd,Trim(Copy(s,2,Length(s))),i);
          end;
          inc(i);
          s := axprovider.dbm.gf.GetnthString(dlist,i);
        end;
        if depfrm<>'' then
        begin
          Delete(depfrm,1,1);
          fd.DepFrames := depfrm;
          AddToPopParentList(fd.DepFrames,fd.FieldName);
        end;
      end else
      begin
        n := dnode.ChildNodes.FindNode('sf');
        if n <> nil then begin
          fd.DepFrames := vartostr(n.NodeValue);
          AddToPopParentList(fd.DepFrames,fd.FieldName);
        end;
        if dnode <> nil then n := dnode.ChildNodes.FindNode('e');
        if not assigned(n) then n := dnode.ChildNodes.FindNode('s');
        if not assigned(n) then n := dnode.ChildNodes.FindNode('f');
        if not assigned(n) then n := dnode.ChildNodes.FindNode('ag');
        if not assigned(n) then n := dnode.ChildNodes.FindNode('fg');
        if not assigned(n) then n := dnode.ChildNodes.FindNode('dc');
        if not assigned(n) then n := dnode.ChildNodes.FindNode('lbl');
        if n=nil then exit;
        if Not Assigned(fd.deps) then
           fd.deps:=TStringList.create;
        fd.deps.Add(fd.FieldName);
        i := 0;
        n := nil;
        while True do
        begin
          fnode :=XML.DocumentElement.ChildNodes.FindNode(fd.deps.Strings[i]);
          if fnode <> nil then dnode := fnode.ChildNodes.FindNode('a58')
          else dnode := nil;
          n := nil;
          if dnode <> nil then n := dnode.ChildNodes.FindNode('e');
          if assigned(n) then
          begin
            dlist := vartostr(n.NodeValue);
            AddDeps(fd,dlist,i);
          end;
          if dnode<> nil then n := dnode.ChildNodes.FindNode('s');
          if assigned(n) then
          begin
            dlist := vartostr(n.NodeValue);
            AddDeps(fd,dlist,i);
          end;
          if dnode<> nil then n := dnode.ChildNodes.FindNode('f');
          if assigned(n) then
          begin
            dlist := vartostr(n.NodeValue);
            AddDeps(fd,dlist,i);
          end;
          if dnode<> nil then n := dnode.ChildNodes.FindNode('ag');
          if assigned(n) then
          begin
            dlist := vartostr(n.NodeValue);
            AddDeps(fd,dlist,i);
          end;
          if dnode<> nil then n := dnode.ChildNodes.FindNode('fg');
          if assigned(n) then
          begin
            dlist := vartostr(n.NodeValue);
            AddfgDeps(fd,dlist);
          end;
          if dnode<> nil then begin
            n := dnode.ChildNodes.FindNode('dc');
            if assigned(n) then begin
              dlist := vartostr(n.NodeValue);
              AddDcNameDeps(fd,dlist,i);
            end;
          end;
          if dnode<> nil then n := dnode.ChildNodes.FindNode('lbl');
          if assigned(n) then
          begin
            dlist := trim(vartostr(n.NodeValue));
            AddLabelDeps(fd,dlist);
          end;
          inc(i);
          if i = fd.deps.Count then break;
        end;
        if fd.deps.Count>0 then
          fd.deps.Delete(0);
      end;
    end;
  end;
  if axprovider.dbm.gf.isservice then
  begin
    dnode := x.ChildNodes.FindNode('a66');
    if dnode <> nil then
    begin
      if dnode.IsTextElement then
      begin
        dlist := VarToStr(dnode.NodeValue);
        if pos('~',dlist) > 0 then
        begin
           dlist := copy(dlist,1,pos('~',dlist)-1);
           Delete(dlist,1,pos(',',dlist));
           ParentList.Add(fd.FieldName+'='+dlist)
        end else
        begin
           if  pos(',',dlist) > 0 then
           begin
             Delete(dlist,1,pos(',',dlist));
             ParentList.Add(fd.FieldName+'='+dlist);
           end;
        end;
      end;
    end;
  end;
end;

procedure TStructDef.AddDeps(fd:pfld;dlist:String;fldindex:integer);
var j,ind : integer;
    s: String;
    depfd:pfld;
begin
  j := 1;
  while true do
  begin
    s := axprovider.dbm.gf.GetnthString(dlist,j);
    if (s = '')  then break;
    if copy(s,1,1) = '#' then
      Delete(s,1,1);
    if fd.deps.IndexOf(s)=-1 then
      fd.deps.Add(s);
    if fldIndex = 0 then begin
      ind := ParentList.IndexOfName(s);
      if ind=-1 then
        ParentList.Add(s+'='+fd.FieldName)
      else
        ParentList.ValueFromIndex[ind] := ParentList.ValueFromIndex[ind]+','+fd.FieldName;
    end;
    depfd:=getfield(s);
    inc(j);
  end;
end;

procedure TStructDef.GetDetMap(fd:pFld;x:ixmlnode);
var n,dn,mn : ixmlnode;
    i : integer;
begin
  n := x.ChildNodes.FindNode(xml_DetStruct);
  if n = nil then exit;
  dn := n.ChildNodes.FindNode(xml_DetTransid);
  if dn = nil then begin
    n:=nil;
    exit;
  end;
  fd.DetTransid := Trim(VarToStr(dn.NodeValue));
  if pos(','+Trim(inttostr(fd.FrameNo))+',',','+MastFrameNos+',') = 0 then
    MastFrameNos := MastFrameNos+','+Trim(IntToStr(fd.FrameNo));
  dn := n.ChildNodes.FindNode(xml_DetMap);
  if dn <> nil  then begin
    if dn.HasAttribute('cond') then
      fld.DetCondition := VarToStr(dn.Attributes['cond']);
    if dn.ChildNodes.Count > 0 then begin
      fd.detmap := TStringList.Create;
      for i := 0 to dn.ChildNodes.Count - 1 do begin
        mn := dn.ChildNodes[i];
        fd.detmap.Add(mn.NodeName+'='+Trim(VarToStr(mn.Attributes['tfld'])));
      end;
    end;
  end;
  if fd.DetTransid <> '' then HasDetStruct := True;
  n:=nil;dn:=nil;mn:=nil;
end;

function TStructDef.GetParents(fldname:String):String;
var ind : integer;
begin
  result := '';
  ind := ParentList.IndexOfName(fldname);
  if ind>-1 then
    result := ParentList.ValueFromIndex[ind];
end;

function TStructDef.CreatePopGrid(x:ixmlnode):Integer;
var pnode,anode : ixmlnode;
    i : integer;
    pfield : String;
begin
  result := -1;
  pnode := x.ChildNodes.FindNode(xml_Popup);
  if pnode = nil then exit;
  if vartostr(pnode.Attributes['pop']) <> 't' then exit;
  New(popGrid);
  popgrid.popname := pnode.NodeName;
  popgrid.FrameNo := StrToInt(vartostr(x.ChildValues[xml_FrameNo]));
  popgrid.Heading := vartostr(pnode.ChildValues[xml_Heading]);
  popgrid.Parent := vartostr(pnode.ChildValues[xml_ParentDC]);
  popgrid.ParentFrameNo := StrToInt(Copy(popgrid.Parent,3,3));
  popgrid.ParentField := vartostr(pnode.ChildValues[xml_ParentFlds]);
  popgrid.Popcond := vartostr(pnode.ChildNodes[xml_PopCondition].Attributes['cond']);
  popgrid.ShowButtons := vartostr(pnode.ChildValues[xml_ShowButton]);
  popgrid.AutoShow := (vartostr(pnode.ChildValues[xml_PopAutoShow])='True');
  popgrid.AutoFill := nil;
  anode := pnode.ChildNodes[xml_AutoFill];
  popgrid.FirmBind := (vartostr(anode.Attributes['firm'])='t');
  popgrid.AutoFillFld := (vartostr(anode.Attributes['auto'])='t');
  popgrid.keyCols := vartostr(anode.Attributes['kcol']);
  if anode.HasChildNodes then begin
    popGrid.AutoFill := TStringList.Create;
    for i := 0 to anode.ChildNodes.Count - 1 do
      popGrid.AutoFill.Add(vartostr(anode.ChildNodes[i].NodeValue));
  end;
  anode := pnode.ChildNodes[xml_DispSum];
  popgrid.DispField := vartostr(anode.Attributes['disp']);
  popgrid.SumFormat := vartostr(anode.Attributes['sum']);
  popgrid.Delimiter := vartostr(anode.Attributes['deli']);
  popGrids.Add(popgrid);
  result := popGrids.Count-1;
  i := 1;
  pfield := Trim(axprovider.dbm.gf.GetnthString(popgrid.ParentField,i));
  while pfield <> '' do begin
    fld := GetField(pfield);
    if assigned(fld) then begin
      fld.PopIndex := result;
      fld.IsParentField := True;
    end;
    inc(i);
    pfield := Trim(axprovider.dbm.gf.GetnthString(popgrid.ParentField,i));
  end;
  fld := GetField(popgrid.DispField);
  if assigned(fld) then begin
    fld.PopIndex := result;
    fld.DispField := True;
  end;
  fld:=GetField(popgrid.ShowButtons);
  if assigned(fld) then begin
    fld.PopIndex := result;
    fld.IsBtnField := True;
    popgrid.PopAt:=fld.orderno;
  end;

  if assigned(fld) then
     GetFrame(popgrid.ParentFrameNo).PopParent := result;
end;

Procedure TStructDef.AddToPopParentList(DepFrames,FldName:String);
var i,ind : integer;
    fno,fname : String;
begin
  i := 1;
  fno := Trim(axprovider.dbm.gf.GetnthString(DepFrames,i));
  while fno <> '' do begin
    fname := 'f'+fno;
    ind := PopParentList.IndexOfName(fname);
    if ind = -1 then
      PopParentList.Add(fname+'='+Fldname)
    else
      PopParentList.ValueFromIndex[ind] := PopParentList.ValueFromIndex[ind]+','+FldName;
    inc(i);
    fno := Trim(axprovider.dbm.gf.GetnthString(DepFrames,i));
  end;
end;

function TStructDef.GetPopParents(frmno:Integer):String;
var ind : integer;
    fname : String;
begin
  result := '';
  fname := 'f'+inttostr(frmno);
  ind := PopParentList.IndexOfName(fname);
  if ind>-1 then
    result := PopParentList.ValueFromIndex[ind];
end;

function TStructDef.GetLastFieldIndex(Frameno:Integer): integer;
var i:integer;
begin
  result:=-1;
  for i:=0 to flds.Count-1 do begin
    if pfld(flds[i]).FrameNo = FrameNo then
      result := i
    else if pfld(flds[i]).FrameNo > FrameNo then
      break;
  end;
end;

function TStructDef.GetFirstEnabledFldName(Frameno:Integer): String;
var i:integer;
begin
  result:='';
  for i:=0 to flds.Count-1 do begin
    if (FrameNo=pfld(flds[i]).FrameNo) and (not pfld(flds[i]).hidden) and (not pfld(flds[i]).readonly) then begin
      result:=pfld(flds[i]).FieldName;
      break;
    end;
  end;
end;

procedure TStructDef.GetPickLists(fd:pFld);
var i,j,tpos:integer;
    f,f1,captions,fnames,fn, SQLText,caps:String;
    firsttime : Boolean;
begin
  SQLText:=fd.SQL.Text;
  i := pos('dynamicfilter',lowercase(sqlText));
  fd.txtSelection := i>0;
  if not fd.txtSelection then exit;
  if pos('dynamicfilterword',lowercase(sqlText)) > 0 then
    fd.WordSearch := True;
  fd.pickfields:=TStringList.create;
  fd.pickcaptions:=TStringList.create;
  firsttime := true;
  fn := '';
  captions := '';
  caps := '';
  while i > 0 do
  begin
    captions := copy(sqltext,i,length(sqlText));
    j := pos('}',captions);
    captions := trim(copy(sqlText,i-1,j));
    if pos('dynamicfilterword',lowercase(captions)) > 0 then
      delete(captions,1,18)
    else
      delete(captions,1,14);
    tpos := pos('~',captions);
    if tpos > 0 then begin
     fnames := trim(copy(captions, 1, tpos-1));
     fd.pickfields.Add(fnames);
     if firsttime then
     begin
       fn := fnames;
       caps := Trim(copy(captions,tpos+1,length(captions)));
       firsttime := False;
     end;
    end;
    Delete(SQLText,1,i+j);
    i := pos('dynamicfilter',lowercase(sqlText));
  end;

  i := 1;
  while True do
  begin
    f := axprovider.dbm.gf.GetNthString(fn,i);
    if f = '' then break;
    f1 := axprovider.dbm.gf.GetNthString(caps,i);
    if f1 = '' then f1 := f;
    fd.pickcaptions.add(f1);
    inc(i);
 end;
end;

procedure TStructDef.GetDropDownPickLists(fd:pFld);
var i,j,cpos, n:integer;
    s,s1,s2,ts, f, SQLText:String;
begin
  SQLText:=fd.SQL.Text;
  i := pos('dynamicfilter',lowercase(sqlText));
  //fd.txtSelection := i>0;
  if i>0 then exit;
  fd.pickfields:=TStringList.create;
  fd.pickcaptions:=TStringList.create;

  s := Trim(lowercase(sqltext));
  s := axprovider.dbm.gf.FindAndReplace(s,'  ',' ');
  s1 := copy(s,8,length(s));
  n := 1;
  s := Trim(axprovider.dbm.gf.getnthstring(s1,n))+' ';
  s2 := axprovider.dbm.gf.getnthstring(s,1,' ');
  if axprovider.dbm.gf.IsKeyWord(s2) then
  begin
    s := Trim(axprovider.dbm.gf.getnthstring(s,2,' '));
  end
  else s := s2;
  cpos := pos(' as ', s);
  if cpos = 0 then
    cpos := pos(' ',s);
  if cpos > 0 then
    s := copy(s,1,cpos-1);
  i := 1;
  while True do
  begin
    f := axprovider.dbm.gf.GetNthString(s,i);
    if f = '' then break;
    fd.pickfields.add(f);
    inc(i);
  end;
end;

procedure TStructDef.UpdateHasGridParents;
var i,j:integer;
    fd, dfd:pFld;
    fm:pFrm;
begin
  for i := 0 to flds.count - 1 do begin
    fd:=pfld(flds[i]);
    if not assigned(fd.dependents) then continue;
    for j:=0 to fd.dependents.count-1 do begin
      dfd := getfield(fd.dependents[j]);
      if (not assigned(dfd)) then begin
        axprovider.dbm.gf.dodebug.msg('Field '+fd.dependents[j]+' dependent of '+fd.FieldName+' is not found');
        continue;
      end;
      if (fd.AsGrid) and (fd.FrameNo=dfd.FrameNo) then
        dfd.HasGridParents:=true
      else begin
        fm:=GetFrame(dfd.FrameNo);
        if fm.Popup then dfd.HasGridParents:=true;
      end;
    end;
  end;
end;

procedure TStructDef.CreateOldFiledsList;
  var i : integer;
      s : String;
      f : pFld;
begin
  i:=1;
  while true do begin
    s:=axprovider.dbm.gf.getnthstring(oldfieldslist,i);
    if s='' then break;
    f := getfield(copy(s, 4, length(s)));
    if assigned(f) then
    begin
      oldfields.Add(s);
    end;
    inc(i);
  end;
  oldfieldslist := '';
end;

Procedure TStructDef.OrderDeps;
var i, j:integer;
    fd, dfd:pFld;
    s:String;
begin
  for i := 0 to flds.count - 1 do begin
    fd:=pfld(flds[i]);
    if not assigned(fd.dependents) then continue;
    for j:=0 to fd.dependents.count-1 do begin
      dfd := getfield(fd.dependents[j]);
      if (fd.AsGrid) and (fd.FrameNo=dfd.FrameNo) then
        dfd.HasGridParents:=true;
      if dfd.PopIndex>-1 then begin
        s:=axprovider.dbm.gf.LeftPad(inttostr(fd.FrameNo), 4, '0');
        s:=s+axprovider.dbm.gf.LeftPad(inttostr(pPopGrid(popgrids[dfd.PopIndex]).popat), 4, '0');
        s:=s+axprovider.dbm.gf.LeftPad(inttostr(dfd.orderno), 4, '0');
      end else begin
        s:=axprovider.dbm.gf.LeftPad(inttostr(dfd.FrameNo), 4, '0');
        s:=s+axprovider.dbm.gf.LeftPad(inttostr(dfd.orderno), 4, '0')+'0000';
      end;
      fd.dependents[j]:=s+fd.DependentTypes[j+1]+fd.dependents[j];
    end;
    fd.dependents.Sort;
    fd.DependentTypes:='';
    for j:=0 to fd.dependents.count-1 do begin
      s:=fd.dependents[j];
      delete(s,1,12);
      fd.DependentTypes:=fd.DependentTypes+s[1];
      delete(s,1,1);
      fd.dependents[j]:=s;
    end;
  end;
end;

procedure TStructDef.AddFgDeps(fd:pfld;dlist:String);
var j,ind : integer;
    s: String;
    depfd:pfld;
begin
  j := 1;
  while true do
  begin
    s := axprovider.dbm.gf.GetnthString(dlist,j);
    if (s = '')  then break;
    s := '#'+trim(s);
    if fd.deps.IndexOf(s)=-1 then
      fd.deps.Add(s);
    inc(j);
  end;
end;

procedure TStructDef.AddLabelDeps(fd:pfld;dlist:String);
var j,ind : integer;
    s, tmp: String;
    depfd:pfld;
begin
  j := 1;
  while true do
  begin
    s := axprovider.dbm.gf.GetnthString(dlist,j);
    if (s = '')  then break;
    tmp := '~'+trim(s);
    if fd.deps.IndexOf(s)=-1 then
      fd.deps.Add(tmp);
//    if not (pos(','+s+',',','+DynamicLabels+',') > 0) then
//    begin
//      if DynamicLabels = '' then
//        DynamicLabels := s
//      else
//        DynamicLabels := DynamicLabels + ',' + s;
//    end;
    inc(j);
  end;
end;

procedure TStructDef.GetDispHyperDet(fd:pfld);
var hposst, hposend, i, j, epos : integer;
    hstr, sname, paramdet, pstr, nname, pn, pv, ostr : string;
    hnode, hpnode, hcnode : ixmlnode;
    typestr,popstr, refstr, loadstr : string;
begin
  hposst := pos('<h>',lowercase(fd.displaydetail.text));
  if hposst = 0 then exit;
  hposend := pos('</h>',lowercase(fd.displaydetail.text));
  hstr := Copy(fd.displaydetail.Text,hposst+3,hposend-hposst-3);
  hstr := axprovider.dbm.gf.FindAndReplace(hstr,'#$D#$A','');
  sname:=''; paramdet:=''; typestr:='';popstr:='';refstr:='';loadstr:='';
  j := 1;
  ostr := axprovider.dbm.gf.GetNthString(hstr,j);
  while ostr <>'' do begin
    epos := pos('=',ostr);
    pn := lowercase(trim(copy(ostr,1,epos-1)));
    pv := copy(ostr,epos+1,length(ostr));
    if pn = 'type' then
      typestr := lowercase(Copy(pv,1,1))
    else if pn = 'name' then
      sname := pv
    else if pn = 'param' then
      paramdet := pv
    else if pn = 'popup'  then begin
      if lowercase(copy(pv,1,1)) = 't' then
        popstr := 'True'
      else
        popstr := 'False'
    end else if pn = 'refresh'  then begin
      if lowercase(copy(pv,1,1)) = 't' then
        refstr := 'True'
      else
        refstr := 'False'
    end else if pn = 'load'  then begin
      if lowercase(copy(pv,1,1)) = 't' then
        loadstr := 'True'
      else
        loadstr := 'False'
    end;
    inc(j);
    ostr := axprovider.dbm.gf.GetNthString(hstr,j);
  end;
  sname := typestr+sname;
  if popstr = '' then popstr := 'True';
  if refstr = '' then refstr := 'False';
  if loadstr = '' then refstr := 'False';
  hnode := xml.DocumentElement.ChildNodes.FindNode('hyperlinks');
  if hnode = nil then begin
    hnode := xml.DocumentElement.AddChild('hyperlinks');
    hnode.Attributes['cat'] := 'hyperlinks';
  end;
  nname := 'dhl__'+fd.fieldname;
  hpnode := hnode.ChildNodes.FindNode(nname);
  if assigned(hpnode) then begin
    hnode.ChildNodes.Delete(nname);
  end;
  hpnode := hnode.AddChild(nname);
  hpnode.Attributes['sname'] := sname;
  hpnode.Attributes['source'] := fd.FieldName;
  hpnode.Attributes['pop'] := popstr;
  hpnode.Attributes['refresh'] := refstr;
  hpnode.Attributes['load'] := loadstr;
  i := 1;
  pstr := axprovider.dbm.gf.GetNthString(paramdet,i,'~');
  while pstr<> '' do begin
    pn := Trim(axprovider.dbm.gf.GetNthString(pstr,1,'='));
    pv := Trim(axprovider.dbm.gf.GetNthString(pstr,2,'='));
    nname := 'p'+inttostr(i);
    hcnode := hpnode.AddChild(nname);
    hcnode.Attributes['n'] := pn;
    hcnode.Attributes['v'] := pv;
    inc(i);
    pstr := axprovider.dbm.gf.GetNthString(paramdet,i,'~');
  end;
end;

procedure TStructDef.AddDcNameDeps(fd:pfld;dlist:String;fldidx:Integer);
var j,ind : integer;
    s,s1: String;
    depfd:pfld;
begin
  j := 1;
  while true do
  begin
    s := axprovider.dbm.gf.GetnthString(dlist,j);
    if (s = '')  then break;
    s1 := '$'+s;
    if fd.deps.IndexOf(s1)=-1 then
      fd.deps.Add(s1);
    if fldidx = 0 then begin
      s1 := 'dc'+s;
      ind := ParentList.IndexOfName(s1);
      if ind=-1 then
        ParentList.Add(s1+'='+fd.FieldName)
      else
        ParentList.ValueFromIndex[ind] := ParentList.ValueFromIndex[ind]+','+fd.FieldName;
    end;
    inc(j);
  end;
end;

function  TStructDef.GetFrameNo(fldname : String):integer;
var vFld : pFld;
begin
  result := -1;
  vFld := GetField(FldName);
  if assigned(vFld) then
    result := vFld.FrameNo;
end;

procedure TStructDef.AddToDynamicLabel(lblName:String);
begin
  if not (pos(','+lblName+',',','+DynamicLabels+',') > 0) then
  begin
    if DynamicLabels = '' then
      DynamicLabels := lblName
    else
      DynamicLabels := DynamicLabels + ',' + lblName;
  end;

end;


Function SortFldsDeps(Item1, Item2: Pointer): Integer;
Begin
  result := 0;
  If pFld(Item1).DepOrdNo < pFld(Item2).DepOrdNo Then
    result := -1
  Else If pFld(Item1).DepOrdNo > pFld(Item2).DepOrdNo Then
    result := 1
  Else if pFld(Item1).DepOrdNo = pFld(Item2).DepOrdNo Then
    result := 0;
End;

Procedure TStructDef.GetTableList;
var
  I , TblIdx : Integer;
  FrmNo , StartFldIdx , EndFldIdx : String;
  DC_TableName : String;
  Tmpfrm : pFrm;
  TmpGridList : TStringList;
Begin
  try
    TmpGridList := TStringList.Create;
    for I := 0 to Frames.Count-1 do
    begin
        Tmpfrm := pFrm(Frames[I]);
        DC_TableName := Tmpfrm.TableName;
        FrmNo := InttoStr(Tmpfrm.FrameNo);
        StartFldIdx := InttoStr(Tmpfrm.StartIndex);
        EndFldIdx := InttoStr((Tmpfrm.StartIndex+Tmpfrm.FieldCount-1));
        if Tmpfrm.AsGrid then
          TmpGridList.Add(DC_TableName+'=G,'+FrmNo+','+StartFldIdx+','+EndFldIdx) //tablename=grid/N.grid,frameno,startidx,endidx
        else
        begin
          TblIdx := TableList.IndexOfName(DC_TableName);
          if TblIdx < 0 then
            TableList.Add(DC_TableName+'=N,'+FrmNo+','+StartFldIdx+','+EndFldIdx)
          else
            TableList.Insert(TblIdx+1,DC_TableName+'=N,'+FrmNo+','+StartFldIdx+','+EndFldIdx);
        end;
    end;
    TableList.AddStrings(TmpGridList);
  finally
   begin
    TmpGridList.Free;
    TmpGridList := nil;
   end;
  end;
End;

procedure TStructDef.CreateSelectedFlds(act,sname, fldlist ,visibleDCs,fglist,fillgriddc : string);
  var n : ixmlnode;
begin
  GetDependants := false;
  n := XML.DocumentElement.ChildNodes.FindNode(sname);
  if n = nil then n := XML.DocumentElement.ChildNodes[0];
  xloadstruct(n);
  if act = 'fload' then
  begin
     GetPList := true;
     ForDep := true;
     SetLoadDataXMLforWeb;
     FloadForWeb(fldlist ,visibleDCs,fglist,fillgriddc)
  end else if act = 'dep' then
  begin
     ForDep := true;
     GetDependants := true;
     SetLoadDataXMLforWeb;
     FloadForWeb(fldlist ,visibleDCs,fglist,fillgriddc);
  end else if act = 'dcload' then
  begin
     GetDependants := true;
     SetLoadDataXMLforWeb;
     if fillgriddc <> '' then
     begin
       CreateSelectedFillGridDC(fldlist,'',fillgriddc,fglist);
     end else if fglist <> '' then
     begin
       CreateSelectedFillGridDC(fldlist,visibleDCs,'',fglist);
     end;
  end else if act = 'dcxlload' then
  begin
     axprovider.dbm.gf.dodebug.msg('DC XL Load');
     GetDependants := true;
     SetLoadDataXMLforWeb;
//     CreateSelectedDCFlds(fillgriddc,fldlist);
     GetFrameForWebLoad(fillgriddc);
     CreateSelectedDCFlds(fillgriddc,fldlist);
     axprovider.dbm.gf.dodebug.msg('DC XL Load over');
  end else
  begin
    SetLoadDataXMLforWeb;
  end;
  GetDependants := false;
  quickload := true;
end;

procedure TStructDef.FloadForWeb(fldlist ,visibleDCs,fglist,fillgriddc : string);
  var i,j,k: integer;
      f,dcno,dcno1,dn,local_createddcs,fgdcs,fno : ansistring;
      fnode  : ixmlnode;
      fgcreated : boolean;
begin
  local_createddcs := '';
  fgdcs := '';
  fgcreated := false;
  i:=1;j:=1;k:=1;
  while true do
  begin
    f := axprovider.dbm.gf.GetnthString(fldlist,i);
    if f = '' then break;
    fnode :=XML.DocumentElement.ChildNodes.FindNode(f);
    if fnode <> nil then
    begin
      dcno := 'dc' + vartostr(fnode.Attributes['dcno']);
      dn := dcno+',';
      if pos(dn,local_createddcs) <= 0 then
      begin
         j := strtoint(vartostr(fnode.Attributes['dcno']));
         if k <= j then
         begin
           while k <= j do
           begin
             fgdcs := '';
             dcno1 := 'dc' + vartostr(k);
             if (visibleDCs<>'') and (fglist <> '') then
             begin
                if (pos(dcno1,visibleDCs) > 0) and (pos(dcno1,fglist) > 0) then
                begin
                    fgdcs := CreateSelectedFillGridDCForLoad(fldlist,'',dcno1,fglist);
                    fgcreated := true;
                end;
             end;
             if fgdcs = '' then
             begin
               xload_selected_dc(XML.DocumentElement.ChildNodes.FindNode(dcno1));
               local_createddcs := local_createddcs + dcno1 + ',';
             end else
             begin
                local_createddcs := local_createddcs + fgdcs + ',';
             end;
             inc(k);
           end;
         end;
      end;
      GetFrameForWebLoad(vartostr(fnode.Attributes['dcno']));
      if GetPList then CreateParentFlds;
      xload_selected_field(fnode);
    end;
    inc(i);
  end;
  if not fgcreated then fgdcs := CreateSelectedFillGridDCForLoad(fldlist, visibleDCs,'',fglist);
end;

procedure TStructDef.CreateParentFlds;
  var k : integer;
  f,pfldNames : ansistring;
  fnode : ixmlnode;
begin
  if not assigned(fld) then exit;
  pfldNames := ParentList.Values[fld.FieldName];
  if pfldNames <> '' then
  begin
    k := 1;
    while true do
    begin
      f := axprovider.dbm.gf.GetnthString(pfldNames,k);
      if f = '' then break;
      GetField(f);
      inc(k);
    end;
  end;
end;

procedure TStructDef.SetLoadDataXMLforWeb;
  var k,i  : integer;
  dcno : ansistring;
  fnode : ixmlnode;
begin
  k:=1;
  while true do
  begin
    dcno := 'dc' + vartostr(k);
    fnode := XML.DocumentElement.ChildNodes.FindNode(dcno);
    if fnode = nil then break;
    xload_selected_dc(fnode);
    i := XML.DocumentElement.ChildNodes.IndexOf(fnode);
    while True do
    begin
      fnode := XML.DocumentElement.ChildNodes.Get(i+1);
      if vartostr(fnode.attributes['cat']) = 'field' then
      begin
        xload_selected_field(fnode);
        break;
      end else
      begin
        inc(i);
        if i+1 >= XML.DocumentElement.ChildNodes.Count then break;
      end;
    end;
    inc(k);
  end;
end;

function  TStructDef.CreateSelectedFillGridDCForLoad(fldlist, visibleDCs, fgdc , fglist : string ) : string;
  var i,j,k: integer;
      f,dcno,dn,fgdcs,fgname,dcfldlist,fno : ansistring;
begin
  result := '';
  fgdcs := '';
  k := 0;
  i := 1;
  while true do
  begin
    f := axprovider.dbm.gf.GetnthString(fglist,i);
    if f = '' then break;
    j := pos('`',f);
    dcno := copy(f,1,j-1);
    fgname := copy(f,j+1,length(f));
    if dcno = fgdc then
    begin
       fno := dcno;
       dn := ','+dcno+',';
       Delete(dcno,1,2);
       GetFrameForWebLoad(dcno);
       CreateSelectedDCFlds(dcno,fldlist);
       if fno <>  fgname then
       begin
         xloadfillgrid(XML.DocumentElement.ChildNodes.FindNode(fgname));
         CreateMapFlds;
       end;
       fgdcs := fgdcs + dn ;
       break;
    end else if pos(quotedstr(dcno),visibleDCs) > 0 then
    begin
       dn := ','+dcno+',';
       Delete(dcno,1,2);
       GetFrameForWebLoad(dcno);
       CreateSelectedDCFlds(dcno,fldlist);
       xloadfillgrid(XML.DocumentElement.ChildNodes.FindNode(fgname));
       CreateMapFlds;
       fgdcs := fgdcs + dn + ',' ;
    end;
    inc(i);
  end;
  result := fgdcs;
end;

function  TStructDef.CreateSelectedFillGridDC(fldlist, visibleDCs, fgdc , fglist : string ) : string;
  var i,j,k: integer;
      f,dcno,dn,fgdcs,fgname,dcfldlist,fno : ansistring;
begin
  result := '';
  fgdcs := '';
  k := 0;
  i := 1;
  while true do
  begin
    f := axprovider.dbm.gf.GetnthString(fglist,i);
    if f = '' then break;
    j := pos('`',f);
    dcno := copy(f,1,j-1);
    fgname := copy(f,j+1,length(f));
    if dcno = fgdc then
    begin
       fno := dcno;
       dn := ','+dcno+',';
       Delete(dcno,1,2);
       GetFrameForWebLoad(dcno);
       CreateSelectedDCFlds(dcno,fldlist);
       if fno <>  fgname then
       begin
         xloadfillgrid(XML.DocumentElement.ChildNodes.FindNode(fgname));
         CreateMapFlds;
       end;
       fgdcs := fgdcs + dn ;
       break;
    end else if pos(quotedstr(dcno),visibleDCs) > 0 then
    begin
       dn := ','+dcno+',';
       Delete(dcno,1,2);
       GetFrameForWebLoad(dcno);
       CreateSelectedDCFlds(dcno,fldlist);
       xloadfillgrid(XML.DocumentElement.ChildNodes.FindNode(fgname));
       CreateMapFlds;
       fgdcs := fgdcs + dn + ',' ;
    end;
    inc(i);
  end;
  if (fgdcs = '') and (fldlist <> '') then
  begin
    Delete(fgdc,1,2);
    GetFrameForWebLoad(fgdc);
    CreateSelectedDCFlds(fgdc,fldlist);
  end;
  result := fgdcs;
end;

procedure TStructDef.CreateSelectedFillGridDCOnDemand(fgdc , fglist : string );
  var i,j,k: integer;
      f,dcno,fgname,fno : ansistring;
begin
  k := 1;
  i := 1;
  fgdc := fgdc +',';
  fglist := fglist + ',';
  while true do
  begin
    fno := axprovider.dbm.gf.GetnthString(fgdc,k);
    if fno = '' then break;
    while true do
    begin
      f := axprovider.dbm.gf.GetnthString(fglist,i);
      if f = '' then break;
      j := pos('`',f);
      dcno := copy(f,1,j-1);
      fgname := copy(f,j+1,length(f));
      if quotedstr(dcno) = fno then
      begin
         fno := dcno;
         Delete(dcno,1,2);
         GetFrameForWebLoad(dcno);
         CreateSelectedDCFlds(dcno,'');
         xloadfillgrid(XML.DocumentElement.ChildNodes.FindNode(fgname));
         CreateMapFlds;
         break;
      end;
      inc(i);
    end;
    inc(k);
  end;
end;

procedure TStructDef.GetFrameForWebLoad(fno : ansistring);
  var i : integer;
begin
  i := CreatedDcs.IndexOf(fno);
  if i > -1 then
  begin
    frm := GetFrame(strtoint(fno));
    exit;
  end;
end;

procedure TStructDef.CreateSelectedDCFlds(dc,fldlist : AnsiString);
  var k,i,p  : integer;
  fnode : ixmlnode;
  dcno,f : ansistring;
begin
  dcno := 'dc'+dc;
  axprovider.dbm.gf.dodebug.msg('CreateSelectedDCFlds DC : ' + dcno);
  fnode := XML.DocumentElement.ChildNodes.FindNode(dcno);
  i := XML.DocumentElement.ChildNodes.IndexOf(fnode);
  if i > -1 then
  begin
    k:= i + 1;
    while true do
    begin
      fnode := XML.DocumentElement.ChildNodes.Get(k);
      if (fnode = nil) or (dc <> vartostr(fnode.Attributes['dcno'])) then break;
      xload_selected_field(fnode);
      inc(k);
    end;
  end;
  if not ForDep then
  begin
    i:=1;
    while true do
    begin
      f := axprovider.dbm.gf.GetnthString(fldlist,i);
      if f = '' then break;
      fnode :=XML.DocumentElement.ChildNodes.FindNode(f);
      if fnode <> nil then
      begin
        GetFrameForWebLoad(vartostr(fnode.Attributes['dcno']));
        xload_selected_field(fnode);
      end;
      inc(i);
    end;
  end;
end;

procedure TStructDef.CreateMapFlds;
  var i : integer;
begin
  if assigned(fg) then
  begin
    for i := 0 to fg.Map.Count - 1 do
    begin
      xload_selected_field(XML.DocumentElement.ChildNodes.FindNode(fg.Map.Names[i]));
      FgAutoShowMapFlds := FgAutoShowMapFlds + fg.Map.Names[i] + ',';
    end;
  end;
end;

function TStructDef.CreateFgsInActions(fgname : ansistring) : pFg ;
begin
   try
     xloadfillgrid(XML.DocumentElement.ChildNodes.FindNode(fgname));
     CreateMapFlds;
     result := fg;
   except
     result := nil;
   end;
end;


procedure TStructDef.xload_selected_field(x : ixmlnode);
var dnode, lnode , cnode, pfxnode : ixmlnode;
    i,j,k,l,fldorderno:integer;
    pchar,s , fname , forder: String;
    dcordno,dcfldordno,tmpordno : AnsiString;
begin
  fld := nil;
  if not FldLoadedDuringStructLoad then exit;
  if not assigned(x) then exit;
  if vartostr(x.attributes['cat']) <> 'field' then exit;
  fname := x.ChildValues[xml_name];
  i := CeatedFlds.IndexOf(lowercase(fname));
  if i > -1 then
  begin
     fld:=GetField(fname);
     exit;
  end;
  fldorderno := xml.DocumentElement.ChildNodes.IndexOf(x);
  forder := inttostr(fldorderno);
  new(fld);
  fld.pickfields:=nil;
  fld.pickcaptions:=nil;
  fld.PickListMode := plmNone;
  fld.PickListDef := '';
  fld.PickRecFound := False;
  fld.PickListLevel := 0;
  fld.WordSearch := False;
  fld.UsedQuotedStr := False;
  fld.DataRows:=TStringList.create;
  fld.HasHyperLink := False;
  fld.FieldName := fname;
  if (x.ChildValues[xml_save]='True') then fld.Tablename:=frm.TableName  //and (lowercase(x.ChildValues[xml_datatype])<>'image')
  else fld.Tablename:='';
  fld.SaveValue := x.ChildValues[xml_save]='True';
  if fld.SaveValue then
  begin
     if x.HasAttribute('encrypted') then
       fld.EncryptValue := vartostr(x.attributes['encrypted'])='T'
     else fld.EncryptValue := false;
  end else fld.EncryptValue := false;
  fld.DataType := lowercase(x.ChildValues[xml_datatype]);
  fld.DataType := fld.DataType[1];
  if fld.DataType = 'n' then
  begin
     if x.HasAttribute('customdecimal') then
       fld.CustomDecimal := vartostr(x.attributes['customdecimal'])='T'
     else fld.CustomDecimal := false;
  end;
  fld.Width := x.ChildValues[xml_datawidth];
  fld.Dec := strtoint(vartostr(x.ChildValues[xml_dec]));
  fld.CurDec := fld.Dec;
  if (fld.DataType = 'n') and (fld.CustomDecimal) then
  begin
     if axprovider.dbm.gf.axdecimal <> -1 then
        fld.Dec := axprovider.dbm.gf.axdecimal;
  end;
  fld.DBDec := 0;
  if x.HasAttribute('dbdecimal') then
  begin
    fld.DBDec := strtoint(vartostr(x.attributes['dbdecimal']));
    if fld.DBDec < fld.Dec then
      fld.DBDec := fld.Dec;
  end;
  fld.Caption := vartostr(x.ChildValues[xml_caption]);
//  if pos('{',fld.Caption) > 0 then AddToDynamicLabel('lbl'+fld.FieldName);
  fld.FrameNo := frm.FrameNo;
  fld.FldUX := TStringlist.Create;
  if x.HasAttribute('fieldtype') then
     fld.CustType := vartostr(x.attributes['fieldtype']);
  fld.Empty:=x.ChildValues[xml_empty]='True';
  if (not fld.Empty) and (fld.Caption <> '') then
     fld.Caption := fld.Caption+'*';
  if (fld.DataType = 'i') then
  begin
    ImgFldList.Add(fld.FieldName+'=i') ;
    if not fld.Empty then begin
      if NoEmptyImgFlds = '' then
        NoEmptyImgFlds := fld.FieldName
      else
        NoEmptyImgFlds := NoEmptyImgFlds+','+fld.FieldName;
    end;
    HasImage := true;
  end else if lowercase(fld.FieldName) = 'dc'+Trim(Inttostr(fld.FrameNo))+'_image' then
  begin
    ImgFldList.Add(fld.FieldName+'=c');
    HasViewImgFlds := True;
  end;
  if lowercase(fld.FieldName) = 'dc'+Trim(Inttostr(fld.FrameNo))+'_imagepath' then
    HasImgPath := True;
  fld.NoDuplicate := not(x.ChildValues[xml_duplicate]='True');
  if fld.NoDuplicate then
    PrimaryFields := primaryFields+','+fld.FieldName;
  fld.AsGrid := frm.AsGrid;
  fld.ValExprn := -1;
  try
    fld.cvalexp:=vartostr(x.ChildValues[xml_vexp]);
  except on e:Exception do
    begin
      if assigned(axprovider) then  axprovider.dbm.gf.DoDebug.Log(axprovider.dbm.gf.Axp_logstr+'\uStructDef\xloadfield - '+e.Message);
      fld.cvalexp:=vartostr(x.ChildNodes.FindNode(xml_vexp).Attributes['expr']); // added because of requirement in MyDocs
    end;
  end;
  fld.hidden := x.ChildValues[xml_hide]='True';
  fld.readonly := x.ChildValues[xml_readonly]='True';
  if frm.ReadOnly then
    frm.ReadOnly := fld.readonly;
  {
  if vartostr(x.ChildValues[xml_Tabstop])<>'' then begin               // focus the read only field when the Tabstop is true
    if lowercase(vartostr(x.ChildValues[xml_Tabstop]))='true' then
      fld.Tabstop:=true
    else if lowercase(vartostr(x.ChildValues[xml_Tabstop]))='false' then
      fld.Tabstop:=false;
  end
  else
    fld.Tabstop:=not fld.readonly;
  }
  fld.AllowChange := frm.AllowChange;
  fld.SetCarry := x.childvalues[xml_setcarry]='True';
  fld.ApplyComma := x.ChildValues[xml_applycomma] = 'True';
  fld.OnlyPositive := x.ChildValues[xml_onlypositive] = 'True';
  fld.searchcol:=1;
  if fld.Caption='' then fld.DispName:=fld.FieldName else fld.DispName:=fld.Caption;
  fld.DisplayTotal := x.ChildValues['disptot']='True';
  fld.ClientValidation := x.ChildValues[xml_cValidate]='True';
  fld.listvals:=nil;
  fld.deps:=nil;
  fld.detmap:=nil;
  fld.LinkField:='';
  fld.SourceField:='';
  fld.Sourcetable:='';
  fld.SourceTransid:='';
  fld.Suggestive:=false;
  fld.multiline:=false;
  fld.FromList:=false;
  fld.Refresh:=false;
  fld.Autoselect := false;
  fld.HasParams := false;
  fld.DynamicParams := false;
  fld.DetTransid:='';
  fld.DetCondition:='';
  fld.PopIndex := -1;
  fld.DispField := False;
  fld.IsBtnField := False;
  fld.IsParentField := False;
  fld.Mask:=vartostr(x.ChildValues[xml_mask]);
  fld.HasGridParents:=false;
  fld.Dependents:=nil;
  fld.DependentTypes:='';
  fld.RefreshOnChange := false;
  fld.cl_multiselect := false;
  fld.RefOnChngFlds := '';
  getdeps(fld, x);
  {
  getdetmap(fld, x);
  }
  s := vartostr(x.childvalues[xml_pattern]);
  if (lowercase(s) = 'isalpha') or (lowercase(s) = 'isnumeric') or (lowercase(s) = 'isemail')
      or (lowercase(s) = 'isalphanumeric') or (lowercase(s) = 'isphone') or (lowercase(s) = 'isurl') then
    fld.Pattern:= ''
  else
    fld.Pattern:= s;
  fld.Hint := vartostr(x.ChildValues[xml_Hint]);
  pchar := vartostr(x.ChildValues[xml_pwordchar]);
  if pchar = '' then
    fld.pwchar := #0
  else
    fld.pwchar := AnsiChar(pchar[1]);
  fld.ModeofEntry := lowercase(x.ChildValues[xml_moe]);
  fld.SourceKey := x.ChildValues[xml_sourcekey]='True';
  convertmoe;
  fld.Exprn := -1;
  fld.cexp := vartostr(x.ChildValues[xml_expr]);

  dnode:=x.ChildNodes.FindNode(xml_details);
  fld.SequenceNode := nil;
  fld.prefixflds := nil;
  if lowercase(fld.ModeofEntry)='autogenerate' then
  begin
    HasAutoGenFields := true;
    fld.SequenceNode := dnode.ChildNodes.FindNode(xml_Sequence);
    fld.prefixflds := TStringList.Create;
    for i := 0 to fld.SequenceNode.ChildNodes.Count - 1 do
    begin
      pfxNode := fld.SequenceNode.ChildNodes[i];
      s := Trim(vartostr(pfxNode.ChildValues[xml_PrefixField]));
      if s <> '' then
        fld.prefixflds.add(s);
    end;
  end;
  if assigned(dnode.ChildNodes.FindNode(xml_sgwidth)) then
    fld.sgwidth:= VarToStr(dnode.ChildValues[xml_sgwidth]);
  if vartostr(dnode.ChildValues[xml_sgheight]) <> '' then
    fld.sgheight:= StrToInt(vartostr(dnode.ChildValues[xml_sgheight]));

  fld.cfield := vartostr(dnode.ChildValues[xml_cfield]);
  fld.QSelect:=nil;
  sType := '';

  if fld.ModeofEntry='select' then begin
    fld.SourceField:=vartostr(dnode.ChildValues[xml_source]);
    fld.SourceTransid:=vartostr(dnode.ChildNodes[xml_source].Attributes['cap']);
    if (fld.AsGrid) and (fld.SourceField <> '') and (fld.SourceTransid <> '')   then
      fld.HasHyperLink := True;

    stype := lowercase(vartostr(dnode.ChildNodes[xml_source].Attributes['stype']));
    if stype <> '' then
       stype := ifthen(stype='sql','s','t');
    if fld.SourceKey then begin
      fld.Sourcetable:=dnode.ChildNodes[xml_source].Attributes['table'];
    end else begin
      if (dnode.ChildNodes[xml_source].HasAttribute('scol')) and
        ((vartostr(dnode.ChildNodes[xml_source].Attributes['scol']) <> ''))  then
        fld.searchcol := dnode.ChildNodes[xml_source].Attributes['scol'];
    end;
    lnode:=dnode.ChildNodes.FindNode(xml_list);
    if (assigned(lnode)) and (lnode.ChildNodes.Count>0)  then begin
      stype := 'l';
      {
      fld.QSelect:=axprovider.dbm.GetXDS(nil);
      fld.QSelect.buffered:=true;
      if (not fileexists(axprovider.dbm.gf.startpath+'Structures\'+axprovider.dbm.gf.AppName+'\'+transid+'_'+fld.FieldName+'.cds')) or (axprovider.dbm.gf.isservice) then begin
        fld.QSelect.CDS.CommandText:= axprovider.dbm.gf.nullcds;
        fld.QSelect.Open;
        if fld.QSelect.CDS.RecordCount > 0 then
          fld.QSelect.CDS.Delete;
        for i:=0 to lnode.ChildNodes.count-1 do
          fld.QSelect.CDS.AppendRecord([lnode.ChildNodes[i].NodeValue]);
        if not axprovider.dbm.gf.isservice then
          fld.QSelect.CDS.SaveToFile(axprovider.dbm.gf.startpath+'Structures\'+axprovider.dbm.gf.AppName+'\'+transid+'_'+fld.FieldName+'.cds');
      end else begin
        fld.QSelect.CDS.LoadFromFile(axprovider.dbm.gf.startpath+'Structures\'+axprovider.dbm.gf.AppName+'\'+transid+'_'+fld.FieldName+'.cds');
      end;
      }
      fld.searchcol:=0;
      fld.FromList:=true;
      fld.Autoselect:=dnode.ChildValues[xml_autoselect]='True'; //ch1
    end;
  end else if fld.ModeofEntry='fill' then begin
    fld.LinkField := vartostr(dnode.ChildValues[xml_fparent]);
    fld.sourcetable := vartostr(dnode.Childvalues[xml_sourcetable]);
    fld.SourceField := vartostr(dnode.ChildValues[xml_fsource]);
    fld.Suggestive := dnode.childvalues[xml_suggestive]='True';
  end else if fld.ModeofEntry='accept' then begin
    fld.Suggestive := dnode.childvalues[xml_suggestive]='True';
  end;

  cnode := x.childnodes.FindNode(xml_gui);
  fld.Kind := '';  // 9.5 1
  if assigned(cnode) then begin
    if cnode.HasAttribute('ctype') then begin
      if (vartostr(cnode.Attributes['ctype']) = 'Check list') then
      begin
        fld.ComponentType:='cl';
        fld.cl_multiselect := true;
      end else if (vartostr(cnode.Attributes['ctype']) = 'Check box') then fld.ComponentType:='cb'
      else if (vartostr(cnode.Attributes['ctype']) = 'Radio group') then begin
        fld.ComponentType:='rg';
        fld.Kind := VarToStr(cnode.Attributes['type']);  // 9.5 1
      end
      else if (vartostr(cnode.Attributes['ctype']) = 'Radio button') then fld.ComponentType:='rb'
      else fld.ComponentType:='';
      fld.Separator := ',';
      if vartostr(cnode.Attributes['ctype']) = 'Check list' then
      begin
        if VarTostr(cnode.Attributes['sep']) <> '' then
          fld.Separator := VarTostr(cnode.Attributes['sep']);
        fld.CommaSelection := True;
      end else begin
        fld.CommaSelection := False;
      end;
    end else fld.CommaSelection := False;
    fld.FontStr := vartostr(cnode.Attributes['font']);
    if fld.FontStr = ',,,' then fld.FontStr := '';
    fld.ColorStr := vartostr(cnode.Attributes['color']);
  end else begin
    fld.CommaSelection := False;
    fld.FontStr := '';
    fld.ColorStr := '';
  end;

  //  fld.sql:=vartostr(dnode.ChildValues[xml_sql]);
  fld.txtSelection := false;
  lnode:=dnode.ChildNodes.FindNode(xml_sql);
  fld.sql := nil;
  if assigned(lnode) then
  begin
    if vartostr(lnode.Attributes['mulselect']) = 'true' then fld.cl_multiselect := true;
    fld.Separator := vartostr(lnode.Attributes['mulsel']);
    if fld.Separator = '' then fld.Separator := ',';
    if (not lnode.HasChildNodes) then begin
      if Trim(vartostr(lnode.NodeValue)) <> '' then begin
        fld.sql := TStringList.Create;
        fld.sql.Add(vartostr(lnode.NodeValue));
      end;
    end else begin
  //    fld.SQL := '';
      fld.sql := TStringList.Create;
      for i := 0 to lnode.ChildNodes.Count-1 do
        fld.SQL.Add(vartostr(lnode.ChildNodes[i].NodeValue));
  //      fld.SQL := fld.SQL+vartostr(lnode.ChildNodes[i].NodeValue);
    end;
  end;
  if (assigned(fld.sql)) and  (Trim(fld.sql.Text)<>'') then begin
    fld.QSelect:=axprovider.dbm.GetXDS(nil);
    fld.QSelect.buffered:=true;
    fld.QSelect.CDS.CommandText:=fld.SQL.Text;
    if (pos('{', fld.sql.Text) > 0) or (pos('(:', fld.sql.Text) > 0) then begin
      fld.HasParams := true;
      fld.DynamicParams:=true;
    end else begin
      fld.HasParams:=fld.QSelect.cds.Params.Count > 0;
      fld.DynamicParams:=false;
    end;
    fld.Autoselect:=dnode.ChildValues[xml_autoselect]='True';
    fld.Refresh:=dnode.ChildValues[xml_refresh]='True';

    if (axprovider.dbm.gf.IsService) and (fld.ComponentType<>'') then // in getstructure service, its changing to fromlist for below condition, so its reversing here.
      fld.FromList:=false;

    fld.txtSelection:= (lnode.HasAttribute('txt')) and (lnode.Attributes['txt'] = 't');
    if fld.txtSelection then begin
      HasPickListFlds := True;
      stype := 'p';
      GetPickLists(fld);
    end
    else if (axprovider.dbm.gf.FastDataFlag) and (fld.ModeofEntry = 'select') then GetDropDownPickLists(fld);

  end;
  fld.SelType := stype;
  lnode:=x.ChildNodes.FindNode(xml_searchsql);
  fld.searchsql := nil;
  if assigned(lnode) then begin
    if lnode.ChildNodes.Count > 0 then begin
      fld.searchsql := TStringList.Create;
      for i := 0 to lnode.ChildNodes.Count-1 do
        fld.searchsql.Add(vartostr(lnode.ChildNodes[i].NodeValue));
    end;
  end;
  lnode:=x.ChildNodes.FindNode(xml_displaydetail);
  fld.displaydetail := nil;
  if assigned(lnode) then begin
    if lnode.ChildNodes.Count > 0 then begin
      fld.displaydetail := TStringList.Create;
      for i := 0 to lnode.ChildNodes.Count-1 do
        fld.displaydetail.Add(vartostr(lnode.ChildNodes[i].NodeValue));
      GetDispHyperDet(fld);
    end;
  end;
  //---Set AxRules Properties
  fld.axRule_ValExprn := -1;
  fld.axRule_ValOnSave_ValExprn := -1;
  if assigned(axprovider.dbm.gf.AxRuleNode) then SetAxRulesProps(fld.FieldName);
  //----
  if (not (fld.readonly)) and (not (fld.hidden)) then
    LastFieldno := flds.Count;

  if lowercase(copy(fld.FieldName, 1, 3)) = 'old' then  oldfieldslist := oldfieldslist + fld.FieldName + ',';

  parser.RegisterVar(fld.FieldName, fld.datatype[1],'');
  if fld.cexp <> '' then
    fld.Exprn:=parser.Prepare(fld.cexp);
  if fld.cvalexp<>'' then
    fld.ValExprn:=parser.Prepare(fld.cvalexp);


  if frm.startindex=-1 then frm.StartIndex:=fcount;

  fld.orderno := fldorderno;
  if priorfrmno <> frm.FrameNo then
  begin
     priorfrmno := frm.FrameNo;
     MakeProperStartIndex;
  end;
//  if frm.StartIndex+frm.FieldCount < flds.Count then
//  begin      axprovider.dbm.gf.leftpad(inttostr(frm.FrameNo),2,'0')
    dcordno := axprovider.dbm.gf.leftpad(inttostr(frm.FrameNo),3,'0') + inttostr(fldorderno-1);
    dcfldordno := axprovider.dbm.gf.leftpad(inttostr(frm.FrameNo),3,'0') + forder;
    i := FldCreaedOrder.IndexOf(dcordno);
    if i > -1 then
    begin
      flds.Insert(i+1,fld);
      CeatedFlds.Insert(i+1,lowercase(fname));
      FldCreaedOrder.Insert(i+1,dcfldordno);
    end else
    begin
      k := -1;
      for j := 0 to FldCreaedOrder.Count - 1 do
      begin
        tmpordno := FldCreaedOrder.Strings[j];
        i := strtoint(copy(tmpordno,1,3));
        delete(tmpordno,1,3);
        l := strtoint(tmpordno);
        if (i = frm.FrameNo) then
        begin
          if l = fldorderno - 1 then
          begin
            k := j + 1;
            break;
          end else k := j + 1;
        end;
      end;
      if k > -1 then
      begin
        flds.Insert(k,fld);
        CeatedFlds.Insert(k,lowercase(fname));
        FldCreaedOrder.Insert(k,dcfldordno);
      end else
      begin
        flds.Add(fld);
        CeatedFlds.Add(lowercase(fname));
        FldCreaedOrder.Add(dcfldordno);
      end;
    end;
  {end else
  begin
    flds.Add(fld);
    CeatedFlds.Add(lowercase(fname));
    FldCreaedOrder.Add(forder);
  end;  }
  inc(frm.FieldCount);
  inc(fcount);
end;

procedure TStructDef.MakeProperStartIndex;
  var i, sindex : integer;
      fm : pFrm;
begin
  sindex := 0;
  for i := 1 to frames.Count-1 do
  begin
    fm:=pFrm(Frames[i-1]);
    sindex := sindex + fm.FieldCount;
    pFrm(Frames[i]).StartIndex := sindex;
  end;
end;

procedure TStructDef.xload_selected_dc(x : ixmlnode);
  var s,fno:String;
      i : integer;
begin
  if x = nil then exit;
  fno := x.ChildValues[xml_frameno];
  i := CreatedDcs.IndexOf(fno);
  if i > -1 then
  begin
    frm := GetFrame(strtoint(fno));
    exit;
  end;
  new(frm);
  frm.FrameNo:=strtoint(fno);
  frm.Parent:=0;
  frm.PageNo:=-1;
  frm.PopParent:=-1;
  frm.jsonstr:='';
  frm.mustloadsql := '';
  frm.TableName:=vartostr(x.ChildValues[xml_table]);
  s:=vartostr(x.childvalues[xml_asgrid]);
  gridstring:=gridstring+lowercase(s[1]);
  frm.AsGrid:=s='True';
  s:=vartostr(x.ChildNodes[xml_Popup].Attributes['pop']);
  frm.Popup:=s='t';
  s:=vartostr(x.ChildValues[xml_dcAllowEmpty]);
  frm.AllowEmpty:=s='True';
  // added for AllowChange
  s:=vartostr(x.ChildValues[xml_AllowChange]);
  frm.AllowChange:=s='True';

  s:=vartostr(x.ChildValues[xml_AddDcRows]);
  if trim(s) ='' then frm.AllowAddRow := true
  else frm.AllowAddRow:=s='True';

  s:=vartostr(x.ChildValues[xml_DeleteDcRows]);
  if trim(s) ='' then frm.AllowDeleteRow := true
  else frm.AllowDeleteRow:=s='True';

  frm.ReadOnly := True;
  framecount:=framecount+1;
  framenames:=framenames+','+x.NodeName;
  if SchemaName = '' then
    tables:=tables+','+frm.TableName
  else
    tables:=tables+','+SchemaName+'.'+frm.TableName;
  if frm.FrameNo=1 then
    primarytable:=frm.TableName;
  frm.caption:=vartostr(x.ChildValues[xml_caption]);
  frm.comp:=nil;
  frm.ActRowDeps := vartostr(x.Attributes['rdf']);
  frm.PopIndex := -1;
  if frm.Popup then
    frm.PopIndex := CreatePopGrid(x);
  frm.StartIndex:=-1;
  frm.FieldCount:=0;
  frm.Rowcount := 0;
  frm.HasDataRows := false;
  frames.Add(frm);
  fieldno:=1;
  CreatedDcs.Add(fno);
end;

procedure TStructDef.SetAxRulesProps(fname : string);
  var n : ixmlnode;
begin
  n := nil;
  if assigned(axRule_Validate) then
  begin
    n := axRule_Validate.ChildNodes.FindNode(fname);
    if assigned(n) then
    begin
      try
        fld.axRule_cvalexp:=vartostr(n.NodeValue);
        if assigned(parser) and (fld.axRule_cvalexp<>'') then
           fld.axRule_ValExprn:=parser.Prepare(fld.axRule_cvalexp);
      except on e:Exception do
        begin
          if assigned(axprovider) then  axprovider.dbm.gf.DoDebug.Log(axprovider.dbm.gf.Axp_logstr+'\uStructDef\SetAxRulesProps - '+e.Message);
        end;
      end;
    end;
  end;
  if assigned(axRule_AllowEmpty) then
  begin
    n := axRule_AllowEmpty.ChildNodes.FindNode(fname);
    if assigned(n) then
    begin
       if vartostr(n.NodeValue) = 'T' then
          fld.Empty:=true
       else if vartostr(n.NodeValue) = 'F' then
          fld.Empty:=false;
    end;
  end;
  if assigned(axRule_AllowDuplicate) then
  begin
    n := axRule_AllowDuplicate.ChildNodes.FindNode(fname);
    if assigned(n) then
    begin
       if vartostr(n.NodeValue) = 'T' then
          fld.NoDuplicate:=false
       else if vartostr(n.NodeValue) = 'F' then
          fld.NoDuplicate:=true;
    end;
    if fld.NoDuplicate then
       PrimaryFields := primaryFields+','+fld.FieldName;
  end;
  if assigned(axRule_ValidateOnSave) then
  begin
    n := axRule_ValidateOnSave.ChildNodes.FindNode(fname);
    if assigned(n) then
    begin
      try
        fld.AxRule_ValOnSave_cvalexp:=vartostr(n.NodeValue);
        if assigned(parser) and (fld.AxRule_ValOnSave_cvalexp<>'') then
           fld.axRule_ValOnSave_ValExprn:=parser.Prepare(fld.AxRule_ValOnSave_cvalexp);
      except on e:Exception do
        begin
          if assigned(axprovider) then  axprovider.dbm.gf.DoDebug.Log(axprovider.dbm.gf.Axp_logstr+'\uStructDef\SetAxRulesProps - '+e.Message);
        end;
      end;
    end;
  end;
end;

end.

{
<frameno><subframeno><orderno><fieldname>

Frame SubFrame  Order Name
1       0       5     f1        000100000005f1
1       0       7     f2        000100000007f2
2       0      12     fg1       000200000012fg1
2       0      15     fg2       000200000015fg2
2       1      28     sfg4      000200010028sfg5
2       1      32     sfg5      000200010032sfg5
3       0      42     ft1       000300000042ft1
3       0      48     ft3       000300000048ft3
}
