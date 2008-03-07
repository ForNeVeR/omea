﻿/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

//	WebHelp 5.10.001
var garrSortChar=new Array();
var gaFtsStop=new Array();
var gaFtsStem=new Array();
var gbWhLang=false;

garrSortChar[0] = 0;
garrSortChar[1] = 1;
garrSortChar[2] = 2;
garrSortChar[3] = 3;
garrSortChar[4] = 4;
garrSortChar[5] = 5;
garrSortChar[6] = 6;
garrSortChar[7] = 7;
garrSortChar[8] = 8;
garrSortChar[9] = 40;
garrSortChar[10] = 41;
garrSortChar[11] = 42;
garrSortChar[12] = 43;
garrSortChar[13] = 44;
garrSortChar[14] = 9;
garrSortChar[15] = 10;
garrSortChar[16] = 11;
garrSortChar[17] = 12;
garrSortChar[18] = 13;
garrSortChar[19] = 14;
garrSortChar[20] = 15;
garrSortChar[21] = 16;
garrSortChar[22] = 17;
garrSortChar[23] = 18;
garrSortChar[24] = 19;
garrSortChar[25] = 20;
garrSortChar[26] = 21;
garrSortChar[27] = 22;
garrSortChar[28] = 23;
garrSortChar[29] = 24;
garrSortChar[30] = 25;
garrSortChar[31] = 26;
garrSortChar[32] = 38;
garrSortChar[33] = 45;
garrSortChar[34] = 46;
garrSortChar[35] = 47;
garrSortChar[36] = 48;
garrSortChar[37] = 49;
garrSortChar[38] = 50;
garrSortChar[39] = 33;
garrSortChar[40] = 51;
garrSortChar[41] = 52;
garrSortChar[42] = 53;
garrSortChar[43] = 88;
garrSortChar[44] = 54;
garrSortChar[45] = 34;
garrSortChar[46] = 55;
garrSortChar[47] = 56;
garrSortChar[48] = 115;
garrSortChar[49] = 119;
garrSortChar[50] = 121;
garrSortChar[51] = 123;
garrSortChar[52] = 125;
garrSortChar[53] = 126;
garrSortChar[54] = 127;
garrSortChar[55] = 128;
garrSortChar[56] = 129;
garrSortChar[57] = 130;
garrSortChar[58] = 57;
garrSortChar[59] = 58;
garrSortChar[60] = 89;
garrSortChar[61] = 90;
garrSortChar[62] = 91;
garrSortChar[63] = 59;
garrSortChar[64] = 60;
garrSortChar[65] = 131;
garrSortChar[66] = 148;
garrSortChar[67] = 150;
garrSortChar[68] = 154;
garrSortChar[69] = 158;
garrSortChar[70] = 168;
garrSortChar[71] = 171;
garrSortChar[72] = 173;
garrSortChar[73] = 175;
garrSortChar[74] = 185;
garrSortChar[75] = 187;
garrSortChar[76] = 189;
garrSortChar[77] = 191;
garrSortChar[78] = 193;
garrSortChar[79] = 197;
garrSortChar[80] = 214;
garrSortChar[81] = 216;
garrSortChar[82] = 218;
garrSortChar[83] = 220;
garrSortChar[84] = 225;
garrSortChar[85] = 230;
garrSortChar[86] = 240;
garrSortChar[87] = 242;
garrSortChar[88] = 244;
garrSortChar[89] = 246;
garrSortChar[90] = 252;
garrSortChar[91] = 61;
garrSortChar[92] = 62;
garrSortChar[93] = 63;
garrSortChar[94] = 64;
garrSortChar[95] = 66;
garrSortChar[96] = 67;
garrSortChar[97] = 131;
garrSortChar[98] = 148;
garrSortChar[99] = 150;
garrSortChar[100] = 154;
garrSortChar[101] = 158;
garrSortChar[102] = 168;
garrSortChar[103] = 171;
garrSortChar[104] = 173;
garrSortChar[105] = 175;
garrSortChar[106] = 185;
garrSortChar[107] = 187;
garrSortChar[108] = 189;
garrSortChar[109] = 191;
garrSortChar[110] = 193;
garrSortChar[111] = 197;
garrSortChar[112] = 214;
garrSortChar[113] = 216;
garrSortChar[114] = 218;
garrSortChar[115] = 220;
garrSortChar[116] = 225;
garrSortChar[117] = 230;
garrSortChar[118] = 240;
garrSortChar[119] = 242;
garrSortChar[120] = 244;
garrSortChar[121] = 246;
garrSortChar[122] = 252;
garrSortChar[123] = 68;
garrSortChar[124] = 69;
garrSortChar[125] = 70;
garrSortChar[126] = 71;
garrSortChar[127] = 27;
garrSortChar[128] = 114;
garrSortChar[129] = 28;
garrSortChar[130] = 82;
garrSortChar[131] = 170;
garrSortChar[132] = 85;
garrSortChar[133] = 112;
garrSortChar[134] = 109;
garrSortChar[135] = 110;
garrSortChar[136] = 65;
garrSortChar[137] = 113;
garrSortChar[138] = 223;
garrSortChar[139] = 86;
garrSortChar[140] = 213;
garrSortChar[141] = 29;
garrSortChar[142] = 255;
garrSortChar[143] = 30;
garrSortChar[144] = 31;
garrSortChar[145] = 80;
garrSortChar[146] = 81;
garrSortChar[147] = 83;
garrSortChar[148] = 84;
garrSortChar[149] = 111;
garrSortChar[150] = 36;
garrSortChar[151] = 37;
garrSortChar[152] = 79;
garrSortChar[153] = 229;
garrSortChar[154] = 222;
garrSortChar[155] = 87;
garrSortChar[156] = 212;
garrSortChar[157] = 32;
garrSortChar[158] = 254;
garrSortChar[159] = 251;
garrSortChar[160] = 39;
garrSortChar[161] = 72;
garrSortChar[162] = 97;
garrSortChar[163] = 98;
garrSortChar[164] = 99;
garrSortChar[165] = 100;
garrSortChar[166] = 73;
garrSortChar[167] = 101;
garrSortChar[168] = 74;
garrSortChar[169] = 102;
garrSortChar[170] = 133;
garrSortChar[171] = 93;
garrSortChar[172] = 103;
garrSortChar[173] = 35;
garrSortChar[174] = 104;
garrSortChar[175] = 75;
garrSortChar[176] = 105;
garrSortChar[177] = 92;
garrSortChar[178] = 122;
garrSortChar[179] = 124;
garrSortChar[180] = 76;
garrSortChar[181] = 106;
garrSortChar[182] = 107;
garrSortChar[183] = 108;
garrSortChar[184] = 77;
garrSortChar[185] = 120;
garrSortChar[186] = 199;
garrSortChar[187] = 94;
garrSortChar[188] = 116;
garrSortChar[189] = 117;
garrSortChar[190] = 118;
garrSortChar[191] = 78;
garrSortChar[192] = 131;
garrSortChar[193] = 131;
garrSortChar[194] = 131;
garrSortChar[195] = 131;
garrSortChar[196] = 131;
garrSortChar[197] = 131;
garrSortChar[198] = 131;
garrSortChar[199] = 150;
garrSortChar[200] = 158;
garrSortChar[201] = 158;
garrSortChar[202] = 158;
garrSortChar[203] = 158;
garrSortChar[204] = 175;
garrSortChar[205] = 175;
garrSortChar[206] = 175;
garrSortChar[207] = 175;
garrSortChar[208] = 154;
garrSortChar[209] = 193;
garrSortChar[210] = 197;
garrSortChar[211] = 197;
garrSortChar[212] = 197;
garrSortChar[213] = 197;
garrSortChar[214] = 197;
garrSortChar[215] = 95;
garrSortChar[216] = 197;
garrSortChar[217] = 230;
garrSortChar[218] = 230;
garrSortChar[219] = 230;
garrSortChar[220] = 230;
garrSortChar[221] = 246;
garrSortChar[222] = 227;
garrSortChar[223] = 224;
garrSortChar[224] = 131;
garrSortChar[225] = 131;
garrSortChar[226] = 131;
garrSortChar[227] = 131;
garrSortChar[228] = 131;
garrSortChar[229] = 131;
garrSortChar[230] = 131;
garrSortChar[231] = 150;
garrSortChar[232] = 158;
garrSortChar[233] = 158;
garrSortChar[234] = 158;
garrSortChar[235] = 158;
garrSortChar[236] = 175;
garrSortChar[237] = 175;
garrSortChar[238] = 175;
garrSortChar[239] = 175;
garrSortChar[240] = 154;
garrSortChar[241] = 193;
garrSortChar[242] = 197;
garrSortChar[243] = 197;
garrSortChar[244] = 197;
garrSortChar[245] = 197;
garrSortChar[246] = 197;
garrSortChar[247] = 96;
garrSortChar[248] = 197;
garrSortChar[249] = 230;
garrSortChar[250] = 230;
garrSortChar[251] = 230;
garrSortChar[252] = 230;
garrSortChar[253] = 246;
garrSortChar[254] = 227;
garrSortChar[255] = 250;

gaFtsStop[0] = "a";
gaFtsStop[1] = "about";
gaFtsStop[2] = "after";
gaFtsStop[3] = "against";
gaFtsStop[4] = "all";
gaFtsStop[5] = "also";
gaFtsStop[6] = "among";
gaFtsStop[7] = "an";
gaFtsStop[8] = "and";
gaFtsStop[9] = "are";
gaFtsStop[10] = "as";
gaFtsStop[11] = "at";
gaFtsStop[12] = "be";
gaFtsStop[13] = "became";
gaFtsStop[14] = "because";
gaFtsStop[15] = "been";
gaFtsStop[16] = "between";
gaFtsStop[17] = "but";
gaFtsStop[18] = "by";
gaFtsStop[19] = "can";
gaFtsStop[20] = "come";
gaFtsStop[21] = "do";
gaFtsStop[22] = "during";
gaFtsStop[23] = "each";
gaFtsStop[24] = "early";
gaFtsStop[25] = "for";
gaFtsStop[26] = "form";
gaFtsStop[27] = "found";
gaFtsStop[28] = "from";
gaFtsStop[29] = "had";
gaFtsStop[30] = "has";
gaFtsStop[31] = "have";
gaFtsStop[32] = "he";
gaFtsStop[33] = "her";
gaFtsStop[34] = "his";
gaFtsStop[35] = "however";
gaFtsStop[36] = "in";
gaFtsStop[37] = "include";
gaFtsStop[38] = "into";
gaFtsStop[39] = "is";
gaFtsStop[40] = "it";
gaFtsStop[41] = "its";
gaFtsStop[42] = "late";
gaFtsStop[43] = "later";
gaFtsStop[44] = "made";
gaFtsStop[45] = "many";
gaFtsStop[46] = "may";
gaFtsStop[47] = "me";
gaFtsStop[48] = "med";
gaFtsStop[49] = "more";
gaFtsStop[50] = "most";
gaFtsStop[51] = "near";
gaFtsStop[52] = "no";
gaFtsStop[53] = "non";
gaFtsStop[54] = "not";
gaFtsStop[55] = "of";
gaFtsStop[56] = "on";
gaFtsStop[57] = "only";
gaFtsStop[58] = "or";
gaFtsStop[59] = "other";
gaFtsStop[60] = "over";
gaFtsStop[61] = "several";
gaFtsStop[62] = "she";
gaFtsStop[63] = "some";
gaFtsStop[64] = "such";
gaFtsStop[65] = "than";
gaFtsStop[66] = "that";
gaFtsStop[67] = "the";
gaFtsStop[68] = "their";
gaFtsStop[69] = "then";
gaFtsStop[70] = "there";
gaFtsStop[71] = "these";
gaFtsStop[72] = "they";
gaFtsStop[73] = "this";
gaFtsStop[74] = "through";
gaFtsStop[75] = "to";
gaFtsStop[76] = "under";
gaFtsStop[77] = "until";
gaFtsStop[78] = "use";
gaFtsStop[79] = "was";
gaFtsStop[80] = "we";
gaFtsStop[81] = "were";
gaFtsStop[82] = "when";
gaFtsStop[83] = "where";
gaFtsStop[84] = "which";
gaFtsStop[85] = "who";
gaFtsStop[86] = "with";
gaFtsStop[87] = "you";

gaFtsStem[0] = "ed";
gaFtsStem[1] = "es";
gaFtsStem[2] = "er";
gaFtsStem[3] = "e";
gaFtsStem[4] = "s";
gaFtsStem[5] = "ingly";
gaFtsStem[6] = "ing";
gaFtsStem[7] = "ly";


// as javascript 1.3 support unicode instead of ISO-Latin-1
// need to transfer come code back to ISO-Latin-1 for compare purpose
// Note: Different Language(Code page) maybe need different array:
var gaUToC=new Array();
gaUToC[8364]=128;
gaUToC[8218]=130;
gaUToC[402]=131;
gaUToC[8222]=132;
gaUToC[8230]=133;
gaUToC[8224]=134;
gaUToC[8225]=135;
gaUToC[710]=136;
gaUToC[8240]=137;
gaUToC[352]=138;
gaUToC[8249]=139;
gaUToC[338]=140;
gaUToC[381]=142;
gaUToC[8216]=145;
gaUToC[8217]=146;
gaUToC[8220]=147;
gaUToC[8221]=148;
gaUToC[8226]=149;
gaUToC[8211]=150;
gaUToC[8212]=151;
gaUToC[732]=152;
gaUToC[8482]=153;
gaUToC[353]=154;
gaUToC[8250]=155;
gaUToC[339]=156;
gaUToC[382]=158;
gaUToC[376]=159;

var gsBiggestChar="";
function getBiggestChar()
{
	if(gsBiggestChar.length==0)
	{
		if(garrSortChar.length<256)
			gsBiggestChar=String.fromCharCode(255);
		else
		{
			var nBiggest=0;
			var nBigChar=0;
			for(var i=0;i<=255;i++)
			{
				if(garrSortChar[i]>nBiggest)
				{
					nBiggest=garrSortChar[i];
					nBigChar=i;
				}
			}
			gsBiggestChar=String.fromCharCode(nBigChar);
		}

	}	
	return gsBiggestChar;
}

function getCharCode(str,i)
{
	var code=str.charCodeAt(i)
	if(code>256)
	{
		code=gaUToC[code];
	}
	return code;
}

function compare(strText1,strText2)
{
	if(garrSortChar.length<256)
	{
		var strt1=strText1.toLowerCase();
		var strt2=strText2.toLowerCase();
		if(strt1<strt2) return -1;
		if(strt1>strt2) return 1;
		return 0;
	}
	else
	{
		for(var i=0;i<strText1.length&&i<strText2.length;i++)
		{
			if(garrSortChar[getCharCode(strText1,i)]<garrSortChar[getCharCode(strText2,i)]) return -1;
			if(garrSortChar[getCharCode(strText1,i)]>garrSortChar[getCharCode(strText2,i)]) return 1;
		}
		if(strText1.length<strText2.length) return -1;
		if(strText1.length>strText2.length) return 1;
		return 0;
	}
}
gbWhLang=true;