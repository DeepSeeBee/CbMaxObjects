{
	"patcher" : 	{
		"fileversion" : 1,
		"appversion" : 		{
			"major" : 8,
			"minor" : 1,
			"revision" : 3,
			"architecture" : "x64",
			"modernui" : 1
		}
,
		"classnamespace" : "box",
		"rect" : [ 42.0, 85.0, 1486.0, 913.0 ],
		"bglocked" : 0,
		"openinpresentation" : 0,
		"default_fontsize" : 12.0,
		"default_fontface" : 0,
		"default_fontname" : "Arial",
		"gridonopen" : 1,
		"gridsize" : [ 15.0, 15.0 ],
		"gridsnaponopen" : 1,
		"objectsnaponopen" : 1,
		"statusbarvisible" : 2,
		"toolbarvisible" : 1,
		"lefttoolbarpinned" : 0,
		"toptoolbarpinned" : 0,
		"righttoolbarpinned" : 0,
		"bottomtoolbarpinned" : 0,
		"toolbars_unpinned_last_save" : 0,
		"tallnewobj" : 0,
		"boxanimatetime" : 200,
		"enablehscroll" : 1,
		"enablevscroll" : 1,
		"devicewidth" : 0.0,
		"description" : "",
		"digest" : "",
		"tags" : "",
		"style" : "",
		"subpatcher_template" : "",
		"boxes" : [ 			{
				"box" : 				{
					"id" : "obj-15",
					"maxclass" : "message",
					"numinlets" : 2,
					"numoutlets" : 1,
					"outlettype" : [ "" ],
					"patching_rect" : [ 926.0, 618.0, 199.0, 22.0 ],
					"text" : "to_channel 1 outputs disablecell 6 0"
				}

			}
, 			{
				"box" : 				{
					"id" : "obj-13",
					"maxclass" : "message",
					"numinlets" : 2,
					"numoutlets" : 1,
					"outlettype" : [ "" ],
					"patching_rect" : [ 901.0, 580.0, 190.0, 22.0 ],
					"text" : "to_channel 1 panel focus border 5"
				}

			}
, 			{
				"box" : 				{
					"id" : "obj-8",
					"maxclass" : "message",
					"numinlets" : 2,
					"numoutlets" : 1,
					"outlettype" : [ "" ],
					"patching_rect" : [ 41.0, 503.5, 244.0, 22.0 ],
					"text" : "refresh_latency"
				}

			}
, 			{
				"box" : 				{
					"bgmode" : 0,
					"border" : 0,
					"clickthrough" : 0,
					"enablehscroll" : 0,
					"enablevscroll" : 0,
					"id" : "obj-85",
					"lockeddragscroll" : 0,
					"maxclass" : "bpatcher",
					"name" : "CbChannelStripMixerMatrix.maxpat",
					"numinlets" : 2,
					"numoutlets" : 2,
					"offset" : [ 0.0, 0.0 ],
					"outlettype" : [ "multichannelsignal", "" ],
					"patching_rect" : [ 26.0, 795.0, 1357.0, 236.0 ],
					"presentation" : 1,
					"presentation_rect" : [ -1.0, -3.0, 1421.0, 210.0 ],
					"viewvisibility" : 1
				}

			}
, 			{
				"box" : 				{
					"id" : "obj-83",
					"lastchannelcount" : 0,
					"maxclass" : "live.gain~",
					"numinlets" : 2,
					"numoutlets" : 5,
					"outlettype" : [ "signal", "signal", "", "float", "list" ],
					"parameter_enable" : 1,
					"patching_rect" : [ 123.0, 1062.5, 48.0, 136.0 ],
					"saved_attribute_attributes" : 					{
						"valueof" : 						{
							"parameter_mmin" : -70.0,
							"parameter_longname" : "live.gain~[1]",
							"parameter_mmax" : 6.0,
							"parameter_shortname" : "live.gain~[1]",
							"parameter_type" : 0,
							"parameter_unitstyle" : 4
						}

					}
,
					"varname" : "live.gain~[1]"
				}

			}
, 			{
				"box" : 				{
					"id" : "obj-82",
					"maxclass" : "newobj",
					"numinlets" : 1,
					"numoutlets" : 2,
					"outlettype" : [ "signal", "signal" ],
					"patching_rect" : [ 110.0, 1033.0, 74.0, 22.0 ],
					"text" : "mc.unpack~"
				}

			}
, 			{
				"box" : 				{
					"id" : "obj-81",
					"maxclass" : "newobj",
					"numinlets" : 2,
					"numoutlets" : 1,
					"outlettype" : [ "multichannelsignal" ],
					"patching_rect" : [ 111.0, 749.0, 60.0, 22.0 ],
					"text" : "mc.pack~"
				}

			}
, 			{
				"box" : 				{
					"id" : "obj-75",
					"local" : 1,
					"maxclass" : "ezdac~",
					"numinlets" : 2,
					"numoutlets" : 0,
					"patching_rect" : [ 116.0, 1086.0, 45.0, 45.0 ]
				}

			}
, 			{
				"box" : 				{
					"fontname" : "Arial",
					"fontsize" : 13.0,
					"id" : "obj-58",
					"maxclass" : "message",
					"numinlets" : 2,
					"numoutlets" : 1,
					"outlettype" : [ "" ],
					"patching_rect" : [ 111.0, 645.5, 45.0, 23.0 ],
					"text" : "$1 20"
				}

			}
, 			{
				"box" : 				{
					"fontname" : "Arial",
					"fontsize" : 13.0,
					"id" : "obj-59",
					"maxclass" : "newobj",
					"numinlets" : 2,
					"numoutlets" : 2,
					"outlettype" : [ "signal", "bang" ],
					"patching_rect" : [ 111.0, 674.5, 40.0, 23.0 ],
					"text" : "line~"
				}

			}
, 			{
				"box" : 				{
					"fontname" : "Arial",
					"fontsize" : 13.0,
					"format" : 6,
					"id" : "obj-60",
					"maxclass" : "flonum",
					"maximum" : 10000.0,
					"minimum" : 10.0,
					"numinlets" : 1,
					"numoutlets" : 2,
					"outlettype" : [ "", "bang" ],
					"parameter_enable" : 1,
					"patching_rect" : [ 111.0, 611.5, 54.0, 23.0 ],
					"saved_attribute_attributes" : 					{
						"valueof" : 						{
							"parameter_mmin" : 10.0,
							"parameter_longname" : "flonum",
							"parameter_initial_enable" : 1,
							"parameter_invisible" : 1,
							"parameter_mmax" : 10000.0,
							"parameter_initial" : [ 440 ],
							"parameter_shortname" : "flonum",
							"parameter_type" : 3
						}

					}
,
					"triscale" : 0.9,
					"varname" : "flonum"
				}

			}
, 			{
				"box" : 				{
					"fontname" : "Arial",
					"fontsize" : 13.0,
					"id" : "obj-61",
					"maxclass" : "newobj",
					"numinlets" : 2,
					"numoutlets" : 1,
					"outlettype" : [ "signal" ],
					"patching_rect" : [ 111.0, 709.0, 157.0, 23.0 ],
					"text" : "cycle~ 440."
				}

			}
, 			{
				"box" : 				{
					"id" : "obj-35",
					"maxclass" : "jit.pwindow",
					"numinlets" : 1,
					"numoutlets" : 2,
					"outlettype" : [ "jit_matrix", "" ],
					"patching_rect" : [ 991.0, 233.5, 205.0, 181.0 ]
				}

			}
, 			{
				"box" : 				{
					"id" : "obj-9",
					"maxclass" : "message",
					"numinlets" : 2,
					"numoutlets" : 1,
					"outlettype" : [ "" ],
					"patching_rect" : [ 467.0, 81.0, 139.0, 22.0 ],
					"text" : "image_surface_get_size"
				}

			}
, 			{
				"box" : 				{
					"id" : "obj-3",
					"maxclass" : "message",
					"numinlets" : 2,
					"numoutlets" : 1,
					"outlettype" : [ "" ],
					"patching_rect" : [ 372.0, -44.0, 571.0, 22.0 ],
					"text" : "set_source_rgba 0 0 0 1, new_path, move_to 72 105, line_to 400 400, line_to 135 150, close_path, stroke"
				}

			}
, 			{
				"box" : 				{
					"id" : "obj-12",
					"maxclass" : "message",
					"numinlets" : 2,
					"numoutlets" : 1,
					"outlettype" : [ "" ],
					"patching_rect" : [ 456.0, -157.0, 456.0, 22.0 ],
					"text" : "new_path, move_to 72 105, move_to 197 105, move_to 135 150, close_path, stroke"
				}

			}
, 			{
				"box" : 				{
					"id" : "obj-10",
					"maxclass" : "message",
					"numinlets" : 2,
					"numoutlets" : 1,
					"outlettype" : [ "" ],
					"patching_rect" : [ 321.0, -120.0, 574.0, 22.0 ],
					"text" : "set_source_rgba 0 0 0 1, new_path, move_to 72. 105, line_to 197 105, line_to 135 150, close_path, stroke"
				}

			}
, 			{
				"box" : 				{
					"fontname" : "Arial",
					"fontsize" : 13.0,
					"id" : "obj-4",
					"maxclass" : "newobj",
					"numinlets" : 1,
					"numoutlets" : 2,
					"outlettype" : [ "jit_matrix", "" ],
					"patching_rect" : [ 233.5, 189.0, 147.0, 23.0 ],
					"text" : "jit.mgraphics 1000 1000"
				}

			}
, 			{
				"box" : 				{
					"id" : "obj-11",
					"maxclass" : "message",
					"numinlets" : 2,
					"numoutlets" : 1,
					"outlettype" : [ "" ],
					"patching_rect" : [ 392.0, -7.0, 298.0, 22.0 ],
					"text" : "set_source_rgba 0 0 0 1, ellipse 100 100 40 70, stroke"
				}

			}
, 			{
				"box" : 				{
					"fontname" : "Arial",
					"fontsize" : 13.0,
					"id" : "obj-6",
					"maxclass" : "newobj",
					"numinlets" : 2,
					"numoutlets" : 1,
					"outlettype" : [ "bang" ],
					"patching_rect" : [ 963.0, 135.0, 71.0, 23.0 ],
					"text" : "qmetro 33"
				}

			}
, 			{
				"box" : 				{
					"fontname" : "Arial",
					"fontsize" : 12.0,
					"id" : "obj-30",
					"maxclass" : "message",
					"numinlets" : 2,
					"numoutlets" : 1,
					"outlettype" : [ "" ],
					"patching_rect" : [ 328.0, -83.0, 470.0, 22.0 ],
					"text" : "set_source_rgba 1 1 1 1, paint, set_source_rgba 0 0 0 1, identity_matrix, move_to 0. 0."
				}

			}
, 			{
				"box" : 				{
					"id" : "obj-7",
					"maxclass" : "jit.pwindow",
					"numinlets" : 1,
					"numoutlets" : 2,
					"outlettype" : [ "jit_matrix", "" ],
					"patching_rect" : [ 491.5, 213.75, 680.0, 153.0 ],
					"presentation" : 1,
					"presentation_rect" : [ 5.0, 211.0, 680.0, 153.0 ]
				}

			}
, 			{
				"box" : 				{
					"id" : "obj-5",
					"maxclass" : "message",
					"numinlets" : 2,
					"numoutlets" : 1,
					"outlettype" : [ "" ],
					"patching_rect" : [ 33.0, 11.0, 34.0, 22.0 ],
					"presentation" : 1,
					"presentation_rect" : [ 1422.0, 61.0, 83.0, 22.0 ],
					"text" : "init 8"
				}

			}
, 			{
				"box" : 				{
					"id" : "obj-1",
					"maxclass" : "newobj",
					"numinlets" : 5,
					"numoutlets" : 5,
					"outlettype" : [ "", "", "", "", "" ],
					"patching_rect" : [ 33.0, 69.0, 224.0, 22.0 ],
					"text" : "cb_clrobject"
				}

			}
, 			{
				"box" : 				{
					"id" : "obj-2",
					"maxclass" : "matrixctrl",
					"numinlets" : 1,
					"numoutlets" : 2,
					"outlettype" : [ "list", "list" ],
					"parameter_enable" : 0,
					"patching_rect" : [ 33.0, 135.0, 213.0, 224.0 ],
					"rows" : 8
				}

			}
 ],
		"lines" : [ 			{
				"patchline" : 				{
					"destination" : [ "obj-2", 0 ],
					"source" : [ "obj-1", 0 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-35", 0 ],
					"source" : [ "obj-1", 1 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-4", 0 ],
					"source" : [ "obj-1", 2 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-7", 0 ],
					"source" : [ "obj-1", 3 ],
					"watchpoint_flags" : 5,
					"watchpoint_id" : 10
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-85", 1 ],
					"source" : [ "obj-1", 4 ],
					"watchpoint_flags" : 5,
					"watchpoint_id" : 9
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-4", 0 ],
					"source" : [ "obj-10", 0 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-85", 1 ],
					"source" : [ "obj-13", 0 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-85", 1 ],
					"source" : [ "obj-15", 0 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-1", 2 ],
					"source" : [ "obj-2", 1 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-1", 1 ],
					"source" : [ "obj-2", 0 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-85", 0 ],
					"disabled" : 1,
					"source" : [ "obj-2", 0 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-4", 0 ],
					"source" : [ "obj-3", 0 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-4", 0 ],
					"source" : [ "obj-30", 0 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-1", 3 ],
					"source" : [ "obj-4", 1 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-7", 0 ],
					"source" : [ "obj-4", 0 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-1", 0 ],
					"source" : [ "obj-5", 0 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-59", 0 ],
					"source" : [ "obj-58", 0 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-61", 0 ],
					"source" : [ "obj-59", 0 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-4", 0 ],
					"disabled" : 1,
					"source" : [ "obj-6", 0 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-58", 0 ],
					"source" : [ "obj-60", 0 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-81", 1 ],
					"order" : 0,
					"source" : [ "obj-61", 0 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-81", 0 ],
					"order" : 1,
					"source" : [ "obj-61", 0 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-85", 0 ],
					"source" : [ "obj-81", 0 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-83", 1 ],
					"source" : [ "obj-82", 1 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-83", 0 ],
					"source" : [ "obj-82", 0 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-75", 1 ],
					"source" : [ "obj-83", 1 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-75", 0 ],
					"source" : [ "obj-83", 0 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-1", 4 ],
					"source" : [ "obj-85", 1 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-82", 0 ],
					"source" : [ "obj-85", 0 ]
				}

			}
, 			{
				"patchline" : 				{
					"destination" : [ "obj-4", 0 ],
					"source" : [ "obj-9", 0 ]
				}

			}
 ],
		"parameters" : 		{
			"obj-85::obj-28::obj-1" : [ "live.gain~[13]", "In", 0 ],
			"obj-85::obj-6::obj-3" : [ "vst~[1]", "vst~", 0 ],
			"obj-85::obj-22::obj-3" : [ "vst~[3]", "vst~", 0 ],
			"obj-85::obj-23::obj-2" : [ "live.gain~[10]", "Out", 0 ],
			"obj-60" : [ "flonum", "flonum", 0 ],
			"obj-85::obj-30::obj-3" : [ "vst~[7]", "vst~", 0 ],
			"obj-85::obj-7::obj-1" : [ "live.gain~", "In", 0 ],
			"obj-85::obj-6::obj-1" : [ "live.gain~[4]", "In", 0 ],
			"obj-85::obj-21::obj-3" : [ "vst~[2]", "vst~", 0 ],
			"obj-85::obj-23::obj-3" : [ "vst~[4]", "vst~", 0 ],
			"obj-85::obj-24::obj-2" : [ "live.gain~[11]", "Out", 0 ],
			"obj-85::obj-28::obj-3" : [ "vst~[6]", "vst~", 0 ],
			"obj-85::obj-7::obj-2" : [ "live.gain~[2]", "Out", 0 ],
			"obj-85::obj-22::obj-1" : [ "live.gain~[7]", "In", 0 ],
			"obj-85::obj-24::obj-3" : [ "vst~[5]", "vst~", 0 ],
			"obj-85::obj-30::obj-1" : [ "live.gain~[15]", "In", 0 ],
			"obj-85::obj-6::obj-2" : [ "live.gain~[3]", "Out", 0 ],
			"obj-83" : [ "live.gain~[1]", "live.gain~[1]", 0 ],
			"obj-85::obj-21::obj-1" : [ "live.gain~[6]", "In", 0 ],
			"obj-85::obj-28::obj-2" : [ "live.gain~[14]", "Out", 0 ],
			"obj-85::obj-7::obj-3" : [ "vst~", "vst~", 0 ],
			"obj-85::obj-23::obj-1" : [ "live.gain~[9]", "In", 0 ],
			"obj-85::obj-30::obj-2" : [ "live.gain~[16]", "Out", 0 ],
			"obj-85::obj-21::obj-2" : [ "live.gain~[5]", "Out", 0 ],
			"obj-85::obj-22::obj-2" : [ "live.gain~[8]", "Out", 0 ],
			"obj-85::obj-24::obj-1" : [ "live.gain~[12]", "In", 0 ],
			"parameterbanks" : 			{

			}
,
			"parameter_overrides" : 			{
				"obj-85::obj-28::obj-1" : 				{
					"parameter_longname" : "live.gain~[13]"
				}
,
				"obj-85::obj-23::obj-2" : 				{
					"parameter_longname" : "live.gain~[10]"
				}
,
				"obj-85::obj-7::obj-1" : 				{
					"parameter_longname" : "live.gain~"
				}
,
				"obj-85::obj-6::obj-1" : 				{
					"parameter_longname" : "live.gain~[4]"
				}
,
				"obj-85::obj-24::obj-2" : 				{
					"parameter_longname" : "live.gain~[11]"
				}
,
				"obj-85::obj-7::obj-2" : 				{
					"parameter_longname" : "live.gain~[2]"
				}
,
				"obj-85::obj-22::obj-1" : 				{
					"parameter_longname" : "live.gain~[7]"
				}
,
				"obj-85::obj-30::obj-1" : 				{
					"parameter_longname" : "live.gain~[15]"
				}
,
				"obj-85::obj-6::obj-2" : 				{
					"parameter_longname" : "live.gain~[3]"
				}
,
				"obj-85::obj-21::obj-1" : 				{
					"parameter_longname" : "live.gain~[6]"
				}
,
				"obj-85::obj-28::obj-2" : 				{
					"parameter_longname" : "live.gain~[14]"
				}
,
				"obj-85::obj-23::obj-1" : 				{
					"parameter_longname" : "live.gain~[9]"
				}
,
				"obj-85::obj-30::obj-2" : 				{
					"parameter_longname" : "live.gain~[16]"
				}
,
				"obj-85::obj-21::obj-2" : 				{
					"parameter_longname" : "live.gain~[5]"
				}
,
				"obj-85::obj-22::obj-2" : 				{
					"parameter_longname" : "live.gain~[8]"
				}
,
				"obj-85::obj-24::obj-1" : 				{
					"parameter_longname" : "live.gain~[12]"
				}

			}

		}
,
		"dependency_cache" : [ 			{
				"name" : "CbChannelStripMixerMatrix.maxpat",
				"bootpath" : "./packages/max-sdk-8.0.3/source/charly_beck/CbChannelStrip/m4l/Test",
				"patcherrelativepath" : ".",
				"type" : "JSON",
				"implicit" : 1
			}
, 			{
				"name" : "CbChannelStripChannel.maxpat",
				"bootpath" : "./packages/max-sdk-8.0.3/source/charly_beck/CbChannelStrip/m4l/Test",
				"patcherrelativepath" : ".",
				"type" : "JSON",
				"implicit" : 1
			}
, 			{
				"name" : "FabFilter Pro-Q 3.maxsnap",
				"bootpath" : "~/Documents/Max 8/Snapshots",
				"patcherrelativepath" : "../../../../../../../../../../Users/Audioworkstation/Documents/Max 8/Snapshots",
				"type" : "mx@s",
				"implicit" : 1
			}
, 			{
				"name" : "cb_clrobject.mxe64",
				"type" : "mx64"
			}
 ],
		"autosave" : 0,
		"styles" : [ 			{
				"name" : "AudioStatus_Menu",
				"default" : 				{
					"bgfillcolor" : 					{
						"type" : "color",
						"color" : [ 0.294118, 0.313726, 0.337255, 1 ],
						"color1" : [ 0.454902, 0.462745, 0.482353, 0 ],
						"color2" : [ 0.290196, 0.309804, 0.301961, 1 ],
						"angle" : 270,
						"proportion" : 0.39,
						"autogradient" : 0
					}

				}
,
				"parentstyle" : "",
				"multi" : 0
			}
 ],
		"bgcolor" : [ 0.996078431372549, 0.996078431372549, 0.996078431372549, 1.0 ],
		"editing_bgcolor" : [ 0.996078431372549, 0.996078431372549, 0.996078431372549, 1.0 ]
	}

}
