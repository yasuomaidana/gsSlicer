﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using g3;
using gs.info;

namespace gs
{
	public class MakerbotTests
	{

		public static GCodeFile SimpleFillTest()
		{
			GCodeFileAccumulator fileAccum = new GCodeFileAccumulator();
			GCodeBuilder builder = new GCodeBuilder(fileAccum);

			MakerbotSettings settings = new MakerbotSettings();

			SingleMaterialFFFCompiler cc = new SingleMaterialFFFCompiler(builder, settings, (a,b)=> { return new MakerbotAssembler(a, b); } );

			cc.Begin();

			double StepY = settings.SolidFillPathSpacingMM();

			ToolpathSetBuilder paths = new ToolpathSetBuilder();
			paths.Initialize(cc.NozzlePosition);
			Vector3d currentPos = paths.Position;

			currentPos = paths.AppendZChange(settings.LayerHeightMM, settings.ZTravelSpeed);

			currentPos = paths.AppendTravel(
				new Vector2d(-50, 0), settings.RapidTravelSpeed);

			Vector2d pos = currentPos.xy;
			List<Vector2d> fill = new List<Vector2d>();
			fill.Add(pos);
			for (int k = 0; k < 5; ++k) {
				pos.x += 100; 		fill.Add(pos);
				pos.y += StepY; 	fill.Add(pos);
				pos.x -= 100;		fill.Add(pos);
				pos.y += StepY; 	fill.Add(pos);
			}
			pos.x += 100; 			fill.Add(pos);
			currentPos = paths.AppendExtrude(fill, settings.CarefulExtrudeSpeed);

			cc.AppendPaths(paths.Paths);

			cc.End();

			System.Console.WriteLine("[MakerbotTests] total extrude length: {0}", cc.ExtruderA);
			return fileAccum.File;
		}






		public static GCodeFile SimpleShellsTest()
		{
			GCodeFileAccumulator fileAccum = new GCodeFileAccumulator();
			GCodeBuilder builder = new GCodeBuilder(fileAccum);

			MakerbotSettings settings = new MakerbotSettings();

			SingleMaterialFFFCompiler cc = new SingleMaterialFFFCompiler(builder, settings, (a,b)=> { return new MakerbotAssembler(a, b); } );;

			cc.Begin();

			ToolpathSetBuilder paths = new ToolpathSetBuilder();
			paths.Initialize(cc.NozzlePosition);
			Vector3d currentPos = paths.Position;

			// layer-up
			currentPos = paths.AppendZChange(settings.LayerHeightMM, settings.ZTravelSpeed);

			SequentialScheduler2d scheduler = new SequentialScheduler2d(paths, settings);

			Polygon2d poly = new Polygon2d();
			double r = 10;
			poly.AppendVertex(new Vector2d(-r,-r));
			poly.AppendVertex(new Vector2d(r,-r));
			poly.AppendVertex(new Vector2d(r, r));
			poly.AppendVertex(new Vector2d(-r, r));
			GeneralPolygon2d shape = new GeneralPolygon2d() { Outer = poly };

			Polygon2d hole = Polygon2d.MakeCircle(r / 2, 6, 30*MathUtil.Deg2Rad);
			hole.Reverse();
			shape.AddHole(hole);

			ShellsFillPolygon shells_gen = new ShellsFillPolygon(shape);
			shells_gen.PathSpacing = settings.SolidFillPathSpacingMM();
			shells_gen.ToolWidth = settings.Machine.NozzleDiamMM;
			shells_gen.Layers = 2;
			shells_gen.Compute();

			scheduler.AppendCurveSets(shells_gen.Shells);

			foreach (GeneralPolygon2d infill_poly in shells_gen.InnerPolygons) {
				ParallelLinesFillPolygon infill_gen = new ParallelLinesFillPolygon(infill_poly) {
					InsetFromInputPolygon = false,
					PathSpacing = settings.SolidFillPathSpacingMM(),
					ToolWidth = settings.Machine.NozzleDiamMM
				};
				infill_gen.Compute();
				scheduler.AppendCurveSets(infill_gen.Paths);
			}

			cc.AppendPaths(paths.Paths);

			cc.End();

			System.Console.WriteLine("[MakerbotTests] total extrude length: {0}", cc.ExtruderA);
			return fileAccum.File;
		}






		public static GCodeFile ShellsPolygonTest(GeneralPolygon2d shape)
		{
			GCodeFileAccumulator fileAccum = new GCodeFileAccumulator();
			GCodeBuilder builder = new GCodeBuilder(fileAccum);

			MakerbotSettings settings = new MakerbotSettings();

			SingleMaterialFFFCompiler cc = new SingleMaterialFFFCompiler(builder, settings, (a,b)=> { return new MakerbotAssembler(a, b); } );;

			cc.Begin();

			ToolpathSetBuilder paths = new ToolpathSetBuilder();
			paths.Initialize(cc.NozzlePosition);
			Vector3d currentPos = paths.Position;

			// layer-up
			currentPos = paths.AppendZChange(settings.LayerHeightMM, settings.ZTravelSpeed);

			SequentialScheduler2d scheduler = new SequentialScheduler2d(paths, settings);

			ShellsFillPolygon shells_gen = new ShellsFillPolygon(shape);
			shells_gen.PathSpacing = settings.SolidFillPathSpacingMM();
			shells_gen.ToolWidth = settings.Machine.NozzleDiamMM;
			shells_gen.Layers = 2;
			shells_gen.Compute();

			scheduler.AppendCurveSets(shells_gen.Shells);

			foreach (GeneralPolygon2d infill_poly in shells_gen.InnerPolygons) {
				ParallelLinesFillPolygon infill_gen = new ParallelLinesFillPolygon(infill_poly) {
					InsetFromInputPolygon = false,
					PathSpacing = settings.SolidFillPathSpacingMM(),
					ToolWidth = settings.Machine.NozzleDiamMM
				};
				infill_gen.Compute();
				scheduler.AppendCurveSets(infill_gen.Paths);
			}

			cc.AppendPaths(paths.Paths);

			cc.End();

			System.Console.WriteLine("[MakerbotTests] total extrude length: {0}", cc.ExtruderA);
			return fileAccum.File;
		}







		public static GCodeFile StackedPolygonTest(GeneralPolygon2d shape, int nLayers)
		{
			GCodeFileAccumulator fileAccum = new GCodeFileAccumulator();
			GCodeBuilder builder = new GCodeBuilder(fileAccum);

			MakerbotSettings settings = new MakerbotSettings();

			SingleMaterialFFFCompiler cc = new SingleMaterialFFFCompiler(builder, settings, (a,b)=> { return new MakerbotAssembler(a, b); } );;

			cc.Begin();

			ToolpathSetBuilder paths = new ToolpathSetBuilder();
			paths.Initialize(cc.NozzlePosition);
			Vector3d currentPos = paths.Position;


			ShellsFillPolygon shells_gen = new ShellsFillPolygon(shape);
			shells_gen.PathSpacing = settings.SolidFillPathSpacingMM();
			shells_gen.ToolWidth = settings.Machine.NozzleDiamMM;
			shells_gen.Layers = 2;
			shells_gen.Compute();

			List<FillCurveSet2d> infill_paths = new List<FillCurveSet2d>();
			foreach (GeneralPolygon2d infill_poly in shells_gen.InnerPolygons) {
				ParallelLinesFillPolygon infill_gen = new ParallelLinesFillPolygon(infill_poly) {
					InsetFromInputPolygon = false,
					PathSpacing = settings.SolidFillPathSpacingMM(),
					ToolWidth = settings.Machine.NozzleDiamMM
				};
				infill_gen.Compute();
				infill_paths.AddRange(infill_gen.Paths);
			}

			for (int i = 0; i < nLayers; ++i) {
				// layer-up
				paths.AppendZChange(settings.LayerHeightMM, settings.ZTravelSpeed);

				// add paths
				SequentialScheduler2d scheduler = new SequentialScheduler2d(paths, settings);
				scheduler.AppendCurveSets(shells_gen.Shells);
				scheduler.AppendCurveSets(infill_paths);
			}

			cc.AppendPaths(paths.Paths);
			cc.End();

			System.Console.WriteLine("[MakerbotTests] total extrude length: {0}", cc.ExtruderA);
			return fileAccum.File;
		}





		public static GCodeFile StackedScaledPolygonTest(GeneralPolygon2d shapeIn, int nLayers, double fTopScale)
		{
			if (fTopScale < 0.25 || fTopScale > 1.5)
				throw new Exception("not a good idea?");

			GCodeFileAccumulator fileAccum = new GCodeFileAccumulator();
			GCodeBuilder builder = new GCodeBuilder(fileAccum);

			MakerbotSettings settings = new MakerbotSettings();

			SingleMaterialFFFCompiler cc = new SingleMaterialFFFCompiler(builder, settings, (a,b)=> { return new MakerbotAssembler(a, b); } );;

			cc.Begin();

			for (int i = 0; i < nLayers; ++i ) {
				double t = (double)i / (double)(nLayers-1);
				double scale = MathUtil.Lerp(1, fTopScale, t);

				ToolpathSetBuilder paths = new ToolpathSetBuilder();
				paths.Initialize(cc.NozzlePosition);
				Vector3d currentPos = paths.Position;

				// layer-up
				currentPos = paths.AppendZChange(settings.LayerHeightMM, settings.ZTravelSpeed);

				SequentialScheduler2d scheduler = new SequentialScheduler2d(paths, settings);

				GeneralPolygon2d shape = new GeneralPolygon2d(shapeIn);
				shape.Scale(scale*Vector2d.One, Vector2d.Zero);

				ShellsFillPolygon shells_gen = new ShellsFillPolygon(shape);
				shells_gen.PathSpacing = settings.SolidFillPathSpacingMM();
				shells_gen.ToolWidth = settings.Machine.NozzleDiamMM;
				shells_gen.Layers = 2;
				shells_gen.Compute();

				scheduler.AppendCurveSets(shells_gen.Shells);

				foreach (GeneralPolygon2d infill_poly in shells_gen.InnerPolygons) {
					ParallelLinesFillPolygon infill_gen = new ParallelLinesFillPolygon(infill_poly) {
						InsetFromInputPolygon = false,
						PathSpacing = settings.SolidFillPathSpacingMM(),
						ToolWidth = settings.Machine.NozzleDiamMM
					};
					infill_gen.Compute();
					scheduler.AppendCurveSets(infill_gen.Paths);
				}

				cc.AppendPaths(paths.Paths);

			}

			cc.End();

			System.Console.WriteLine("[MakerbotTests] total extrude length: {0}", cc.ExtruderA);
			return fileAccum.File;
		}








		public static GCodeFile SliceMeshTest(DMesh3 mesh)
		{
			GCodeFileAccumulator fileAccum = new GCodeFileAccumulator();
			GCodeBuilder builder = new GCodeBuilder(fileAccum);
			MakerbotSettings settings = new MakerbotSettings();

			MeshPlanarSlicer slicer = new MeshPlanarSlicer();
			slicer.LayerHeightMM = settings.LayerHeightMM;
			slicer.AddMesh(mesh);

			PlanarSliceStack stack = slicer.Compute();
			int nLayers = stack.Slices.Count;

			int RoofFloorLayers = 2;
			double InfillScale = 1.0;
			double[] infill_angles = new double[] { -45, 45 };

			SingleMaterialFFFCompiler cc = new SingleMaterialFFFCompiler(builder, settings, (a,b)=> { return new MakerbotAssembler(a, b); } );;

			cc.Begin();

			for (int i = 0; i < nLayers; ++i) {
				System.Console.WriteLine("Layer {0} of {1}", i, nLayers);

				PlanarSlice slice = stack.Slices[i];

				ToolpathSetBuilder paths = new ToolpathSetBuilder();
				paths.Initialize(cc.NozzlePosition);
				Vector3d currentPos = paths.Position;

				// layer-up
				currentPos = paths.AppendZChange(settings.LayerHeightMM, settings.ZTravelSpeed);

				bool is_infill = (i >= RoofFloorLayers && i < nLayers - RoofFloorLayers-1);
				double fillScale = (is_infill) ? InfillScale : 1.0f;

				SequentialScheduler2d scheduler = new SequentialScheduler2d(paths, settings);

				foreach(GeneralPolygon2d shape in slice.Solids) {
					ShellsFillPolygon shells_gen = new ShellsFillPolygon(shape);
					shells_gen.PathSpacing = settings.SolidFillPathSpacingMM();
					shells_gen.ToolWidth = settings.Machine.NozzleDiamMM;
					shells_gen.Layers = 2;
					shells_gen.Compute();

					scheduler.AppendCurveSets(shells_gen.Shells);

					foreach (GeneralPolygon2d infill_poly in shells_gen.InnerPolygons) {
						ParallelLinesFillPolygon infill_gen = new ParallelLinesFillPolygon(infill_poly) {
							InsetFromInputPolygon = false,
							PathSpacing = fillScale * settings.SolidFillPathSpacingMM(),
							ToolWidth = settings.Machine.NozzleDiamMM,
							AngleDeg = infill_angles[i % infill_angles.Length]
						};
						infill_gen.Compute();
						scheduler.AppendCurveSets(infill_gen.Paths);
					}
				}

				cc.AppendPaths(paths.Paths);

			}

			cc.End();

			System.Console.WriteLine("[MakerbotTests] total extrude length: {0}", cc.ExtruderA);
			return fileAccum.File;
		}








		public static GCodeFile SliceMeshTest_Roofs(DMesh3 mesh)
		{
			GCodeFileAccumulator fileAccum = new GCodeFileAccumulator();
			GCodeBuilder builder = new GCodeBuilder(fileAccum);
			MakerbotSettings settings = new MakerbotSettings();

			MeshPlanarSlicer slicer = new MeshPlanarSlicer();
			slicer.LayerHeightMM = settings.LayerHeightMM;
			slicer.AddMesh(mesh);

			PlanarSliceStack stack = slicer.Compute();
			int nLayers = stack.Slices.Count;

			settings.SparseLinearInfillStepX = 3.0;

			// [TODO] move to settings...
			int NumShells = 3;
			int InteriorSolidRegionContours = 1;
			double[] infill_angles = new double[] { -45, 45 };
			bool AddSupport = false;

			SingleMaterialFFFCompiler cc = new SingleMaterialFFFCompiler(builder, settings, (a,b)=> { return new MakerbotAssembler(a, b); } );;

			cc.Begin();
			Vector3d CurrentPos = cc.NozzlePosition;


			// compute shells for each layer
			List<ShellsFillPolygon>[] LayerShells = new List<ShellsFillPolygon>[nLayers];
			gParallel.ForEach(Interval1i.Range(nLayers), (layeri) => {
				PlanarSlice slice = stack.Slices[layeri];
				LayerShells[layeri] = new List<ShellsFillPolygon>();

				foreach (GeneralPolygon2d shape in slice.Solids) {
					ShellsFillPolygon shells_gen = new ShellsFillPolygon(shape);
					shells_gen.PathSpacing = settings.SolidFillPathSpacingMM();
					shells_gen.ToolWidth = settings.Machine.NozzleDiamMM;
					shells_gen.Layers = NumShells;
					shells_gen.Compute();
					LayerShells[layeri].Add(shells_gen);
				}
			});


			// should be parameterizable? this is 45 degrees...
			//   (is it? 45 if nozzlediam == layerheight...)
			//double fOverhangAllowance = 0.5 * settings.NozzleDiamMM;
			double fOverhangAllowance = settings.LayerHeightMM / Math.Tan(45 * MathUtil.Deg2Rad);

			List<GeneralPolygon2d>[] LayerSupportAreas = new List<GeneralPolygon2d>[nLayers];
			if (AddSupport) {

				// Compute required support area for each layer
				// Note that this does *not* include thickness allowance, this is
				// the "outer" support-requiring polygon
				gParallel.ForEach(Interval1i.Range(nLayers - 1), (layeri) => {
					PlanarSlice slice = stack.Slices[layeri];
					PlanarSlice next_slice = stack.Slices[layeri + 1];

					List<GeneralPolygon2d> insetPolys = ClipperUtil.MiterOffset(next_slice.Solids, -fOverhangAllowance);
					List<GeneralPolygon2d> supportPolys = ClipperUtil.Difference(insetPolys, slice.Solids);
					LayerSupportAreas[layeri] = supportPolys;
				});
				LayerSupportAreas[nLayers - 1] = new List<GeneralPolygon2d>();

				// now merge support layers. Process is to track "current" support area,
				// at layer below we union with that layers support, and then subtract
				// that layers solids. 
				List<GeneralPolygon2d> prevSupport = LayerSupportAreas[nLayers - 1];
				for (int i = nLayers - 2; i >= 0; --i) {
					PlanarSlice slice = stack.Slices[i];


					// union down
					List<GeneralPolygon2d> combineSupport = null;
					bool dilate = true;
					double fDilate = settings.Machine.NozzleDiamMM * 0.5;
					if (dilate) {
						List<GeneralPolygon2d> a = ClipperUtil.MiterOffset(prevSupport, fDilate);
						List<GeneralPolygon2d> b = ClipperUtil.MiterOffset(LayerSupportAreas[i], fDilate);
						combineSupport = ClipperUtil.Union(a, b);
						combineSupport = ClipperUtil.MiterOffset(combineSupport, -fDilate);
					} else {
						combineSupport = ClipperUtil.Union(prevSupport, LayerSupportAreas[i]);
					}

					// support area we propagate down is combined area minus solid
					prevSupport = ClipperUtil.Difference(combineSupport, slice.Solids);

					// on this layer, we need to leave space for filament, by dilating solid by
					// half nozzle-width and subtracting it
					List<GeneralPolygon2d> dilatedSolid = ClipperUtil.MiterOffset(slice.Solids, settings.Machine.NozzleDiamMM * 0.5);
					combineSupport = ClipperUtil.Difference(combineSupport, dilatedSolid);

					// the actual area we will support is nudged inwards
					//LayerSupportAreas[i] = ClipperUtil.MiterOffset(combineSupport, -fOverhangAllowance);
					LayerSupportAreas[i] = DeepCopy.List(combineSupport);
				}
			} else {
				for (int i = 0; i < nLayers; ++i)
					LayerSupportAreas[i] = new List<GeneralPolygon2d>();
			}


			// generate paths for print area
			for (int i = 0; i < nLayers; ++i) {
				System.Console.WriteLine("Layer {0} of {1}", i, nLayers);

				PlanarSlice slice = stack.Slices[i];

				ToolpathSetBuilder paths = new ToolpathSetBuilder();
				paths.Initialize(CurrentPos);
				Vector3d currentPos = paths.Position;

				// layer-up
				currentPos = paths.AppendZChange(settings.LayerHeightMM, settings.ZTravelSpeed);

				bool is_infill = (i >= settings.FloorLayers && i < nLayers - settings.RoofLayers - 1);
				double fillScale = (is_infill) ? settings.SparseLinearInfillStepX : 1.0f;

				SequentialScheduler2d scheduler = new SequentialScheduler2d(paths, settings);

				// construct region that needs to be solid for "roofs".
				// This is the intersection of infill polygons for the next N layers
				List<GeneralPolygon2d> roof_cover = new List<GeneralPolygon2d>();
				if (is_infill) {
					foreach (ShellsFillPolygon shells in LayerShells[i + 1])
						roof_cover.AddRange(shells.InnerPolygons);

					// If we want > 1 roof layer, we need to look further ahead.
					// The full area we need to print as "roof" is the infill minus
					// the intersection of the infill areas above
					for (int k = 2; k <= settings.RoofLayers; ++k) {
						int ri = i + k;
						if (ri < LayerShells.Length) {
							List<GeneralPolygon2d> infillN = new List<GeneralPolygon2d>();
							foreach (ShellsFillPolygon shells in LayerShells[ri])
								infillN.AddRange(shells.InnerPolygons);

							roof_cover = ClipperUtil.Intersection(roof_cover, infillN);
						}
					}

					// add overhang allowance. Technically any non-vertical surface will result in
					// non-empty roof regions. However we do not need to explicitly support roofs
					// until they are "too horizontal". 
					roof_cover = ClipperUtil.MiterOffset(roof_cover, fOverhangAllowance);
				}



				// construct region that needs to be solid for "floors"
				List<GeneralPolygon2d> floor_cover = new List<GeneralPolygon2d>();
				if (is_infill) {
					foreach (ShellsFillPolygon shells in LayerShells[i-1])
						floor_cover.AddRange(shells.InnerPolygons);

					// If we want > 1 floor layer, we need to look further back.
					for (int k = 2; k <= settings.FloorLayers; ++k) {
						int ri = i - k;
						if (ri > 0) {
							List<GeneralPolygon2d> infillN = new List<GeneralPolygon2d>();
							foreach (ShellsFillPolygon shells in LayerShells[ri])
								infillN.AddRange(shells.InnerPolygons);

							floor_cover = ClipperUtil.Intersection(floor_cover, infillN);
						}
					}

					// add overhang allowance. 
					floor_cover = ClipperUtil.MiterOffset(floor_cover, fOverhangAllowance);
				}


				List<ShellsFillPolygon> curShells = LayerShells[i];
				for (int si = 0; si < curShells.Count; si++) {
					ShellsFillPolygon shells_gen = curShells[si];

					scheduler.AppendCurveSets(shells_gen.Shells);

					// construct infill poly list
					List<GeneralPolygon2d> infillPolys = new List<GeneralPolygon2d>();
					List<GeneralPolygon2d> solidFillPolys = shells_gen.InnerPolygons;
					if (is_infill) {
						infillPolys = shells_gen.InnerPolygons;
						List<GeneralPolygon2d> roofPolys = ClipperUtil.Difference(infillPolys, roof_cover);
						List<GeneralPolygon2d> floorPolys = ClipperUtil.Difference(infillPolys, floor_cover);
						solidFillPolys = ClipperUtil.Union(roofPolys, floorPolys);
						if (solidFillPolys == null)
							solidFillPolys = new List<GeneralPolygon2d>();

						// [TODO] I think maybe we should actually do another set of contours for the
						// solid region. At least one. This gives the solid & infill something to
						// connect to, and gives the contours above a continuous bonding thread

						// subtract solid fill from infill regions. However because we *don't*
						// inset fill regions, we need to subtract (solid+offset), so that
						// infill won't overlap solid region
						if ( solidFillPolys.Count > 0 ) {
							List<GeneralPolygon2d> solidWithBorder = 
								ClipperUtil.MiterOffset(solidFillPolys, settings.Machine.NozzleDiamMM);
							infillPolys = ClipperUtil.Difference(infillPolys, solidWithBorder);
						}
					}

					// fill solid regions
					foreach (GeneralPolygon2d solid_poly in solidFillPolys) {

						List<GeneralPolygon2d> fillPolys = new List<GeneralPolygon2d>() { solid_poly };

						// if we are on an infill layer, and this shell has some infill region,
						// then we are going to draw contours around solid fill so it has
						// something to stick to
						// [TODO] should only be doing this if solid-fill is adjecent to infill region.
						//   But how to determine this? not easly because we don't know which polys
						//   came from where. Would need to do loop above per-polygon
						if ( is_infill && infillPolys.Count > 0 && InteriorSolidRegionContours > 0) {
							ShellsFillPolygon interior_shells = new ShellsFillPolygon(solid_poly);
							interior_shells.PathSpacing = settings.SolidFillPathSpacingMM();
							interior_shells.ToolWidth = settings.Machine.NozzleDiamMM;
							interior_shells.Layers = InteriorSolidRegionContours;
							interior_shells.InsetFromInputPolygon = false;
							interior_shells.Compute();
							scheduler.AppendCurveSets(interior_shells.Shells);
							fillPolys = interior_shells.InnerPolygons;
						}

						// now actually fill solid regions
						foreach (GeneralPolygon2d fillPoly in fillPolys) {
							ParallelLinesFillPolygon solid_gen = new ParallelLinesFillPolygon(fillPoly) {
								InsetFromInputPolygon = false,
								PathSpacing = settings.SolidFillPathSpacingMM(),
								ToolWidth = settings.Machine.NozzleDiamMM,
								AngleDeg = infill_angles[i % infill_angles.Length]
							};
							solid_gen.Compute();
							scheduler.AppendCurveSets(solid_gen.Paths);
						}
					}

					// fill infill regions
					foreach (GeneralPolygon2d infill_poly in infillPolys) {
						ParallelLinesFillPolygon infill_gen = new ParallelLinesFillPolygon(infill_poly) {
							InsetFromInputPolygon = false,
							PathSpacing = fillScale * settings.SolidFillPathSpacingMM(),
							ToolWidth = settings.Machine.NozzleDiamMM,
							AngleDeg = infill_angles[i % infill_angles.Length]
						};
						infill_gen.Compute();
						scheduler.AppendCurveSets(infill_gen.Paths);
					}
				}


				// fill support areas
				foreach (GeneralPolygon2d area in LayerSupportAreas[i]) {

					//DenseLinesFillPolygon support_gen = new DenseLinesFillPolygon(area) {
					//	InsetFromInputPolygon = false,
					//	PathSpacing = 3 * settings.FillPathSpacingMM,
					//	ToolWidth = settings.NozzleDiamMM,
					//	AngleDeg = 90
					//};
					//support_gen.Compute();
					//scheduler.Append(support_gen.Paths);


					ShellsFillPolygon shells_gen = new ShellsFillPolygon(area);
					shells_gen.PathSpacing = settings.SolidFillPathSpacingMM();
					shells_gen.ToolWidth = settings.Machine.NozzleDiamMM;
					shells_gen.InsetFromInputPolygon = false;
					shells_gen.Layers = 1;
					shells_gen.Compute();
					scheduler.AppendCurveSets(shells_gen.Shells);		
				}


				cc.AppendPaths(paths.Paths);
				CurrentPos = cc.NozzlePosition;
			}
			
			cc.End();

			System.Console.WriteLine("[MakerbotTests] total extrude length: {0}", cc.ExtruderA);
			return fileAccum.File;
		}









		public static GCodeFile InfillBoxTest()
		{
			double r = 20;				// box 'radius'
			int HeightLayers = 10;
			int RoofFloorLayers = 2;
			double InfillScale = 6.0;
			double[] infill_angles = new double[] { -45, 45 };
			//double[] infill_angles = new double[] { -45 };
			int infill_layer_k = 0;
			bool enable_rapid = true;
			bool enable_layer_offset = true;

			GCodeFileAccumulator fileAccum = new GCodeFileAccumulator();
			GCodeBuilder builder = new GCodeBuilder(fileAccum);
			MakerbotSettings settings = new MakerbotSettings();


			SingleMaterialFFFCompiler cc = new SingleMaterialFFFCompiler(builder, settings, (a,b)=> { return new MakerbotAssembler(a, b); } );;

			cc.Begin();


			Polygon2d poly = new Polygon2d();
			poly.AppendVertex(new Vector2d(-r, -r));
			poly.AppendVertex(new Vector2d(r, -r));
			poly.AppendVertex(new Vector2d(r, r));
			poly.AppendVertex(new Vector2d(-r, r));
			GeneralPolygon2d gpoly = new GeneralPolygon2d() { Outer = poly };
			List<GeneralPolygon2d> polygons = new List<GeneralPolygon2d>() { gpoly };

			//GeneralPolygon2d sub = new GeneralPolygon2d(Polygon2d.MakeCircle(r / 2, 12));
			////sub.Translate(r * Vector2d.One);
			//List<GeneralPolygon2d> result =
			//	ClipperUtil.PolygonBoolean(polygons, sub, ClipperUtil.BooleanOp.Difference);
			//polygons = result;

			for (int i = 0; i < HeightLayers; ++i) {
				System.Console.WriteLine("Layer {0} of {1}", i, HeightLayers);

				//bool is_infill = (i >= RoofFloorLayers && i < HeightLayers - RoofFloorLayers);
				bool is_infill = (i >= RoofFloorLayers);
				double fillScale = (is_infill) ? InfillScale : 1.0f;

				ToolpathSetBuilder paths = new ToolpathSetBuilder();
				paths.Initialize(cc.NozzlePosition);
				Vector3d currentPos = paths.Position;

				// layer-up
				currentPos = paths.AppendZChange(settings.LayerHeightMM, settings.ZTravelSpeed);

				SequentialScheduler2d scheduler = new SequentialScheduler2d(paths, settings);
                if (is_infill && enable_rapid)
                    scheduler.SpeedHint = SchedulerSpeedHint.Rapid;
                else
                    scheduler.SpeedHint = SchedulerSpeedHint.Careful;

				foreach (GeneralPolygon2d shape in polygons) {
					ShellsFillPolygon shells_gen = new ShellsFillPolygon(shape);
					shells_gen.PathSpacing = settings.SolidFillPathSpacingMM();
					shells_gen.ToolWidth = settings.Machine.NozzleDiamMM;
					shells_gen.Layers = 2;
					shells_gen.Compute();

					scheduler.AppendCurveSets(shells_gen.Shells);

					foreach (GeneralPolygon2d infill_poly in shells_gen.InnerPolygons) {
						ParallelLinesFillPolygon infill_gen = new ParallelLinesFillPolygon(infill_poly) {
							InsetFromInputPolygon = false,
							PathSpacing = fillScale * settings.SolidFillPathSpacingMM(),
							ToolWidth = settings.Machine.NozzleDiamMM,
							AngleDeg = infill_angles[infill_layer_k],
							PathShift = (enable_layer_offset == false || i % 2 == 0) 
								? 0 : (fillScale * settings.SolidFillPathSpacingMM() * (0.5))
						};
						infill_gen.Compute();
						scheduler.AppendCurveSets(infill_gen.Paths);
					}
				}

				cc.AppendPaths(paths.Paths);
				infill_layer_k = (infill_layer_k + 1) % infill_angles.Length;
			}

			cc.End();

			System.Console.WriteLine("[MakerbotTests] total extrude length: {0}", cc.ExtruderA);
			return fileAccum.File;
		}







	}
}
