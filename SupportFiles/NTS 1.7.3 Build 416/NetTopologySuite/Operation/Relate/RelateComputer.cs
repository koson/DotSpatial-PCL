using System.Collections;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Algorithm;
using GisSharpBlog.NetTopologySuite.GeometriesGraph;
using GisSharpBlog.NetTopologySuite.GeometriesGraph.Index;
using GisSharpBlog.NetTopologySuite.Utilities;

namespace GisSharpBlog.NetTopologySuite.Operation.Relate
{
    /// <summary>
    /// Computes the topological relationship between two Geometries.
    /// RelateComputer does not need to build a complete graph structure to compute
    /// the IntersectionMatrix.  The relationship between the geometries can
    /// be computed by simply examining the labelling of edges incident on each node.
    /// RelateComputer does not currently support arbitrary GeometryCollections.
    /// This is because GeometryCollections can contain overlapping Polygons.
    /// In order to correct compute relate on overlapping Polygons, they
    /// would first need to be noded and merged (if not explicitly, at least
    /// implicitly).
    /// </summary>
    public class RelateComputer
    {
        private LineIntersector li = new RobustLineIntersector();
        private PointLocator ptLocator = new PointLocator();
        private GeometryGraph[] arg;     // the arg(s) of the operation
        private NodeMap nodes = new NodeMap(new RelateNodeFactory());                
        private ArrayList isolatedEdges = new ArrayList();        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        public RelateComputer(GeometryGraph[] arg)
        {
            this.arg = arg;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IntersectionMatrix ComputeIM()
        {
            IntersectionMatrix im = new IntersectionMatrix();
            // since Geometries are finite and embedded in a 2-D space, the EE element must always be 2
            im.Set(Locations.Exterior, Locations.Exterior, Dimensions.Surface);

            // if the Geometries don't overlap there is nothing to do
            if (!arg[0].Geometry.EnvelopeInternal.Intersects(arg[1].Geometry.EnvelopeInternal))
            {
                ComputeDisjointIM(im);
                return im;
            }
            arg[0].ComputeSelfNodes(li, false);
            arg[1].ComputeSelfNodes(li, false);

            // compute intersections between edges of the two input geometries
            SegmentIntersector intersector = arg[0].ComputeEdgeIntersections(arg[1], li, false);           
            ComputeIntersectionNodes(0);
            ComputeIntersectionNodes(1);

            /*
             * Copy the labelling for the nodes in the parent Geometries.  These override
             * any labels determined by intersections between the geometries.
             */
            CopyNodesAndLabels(0);
            CopyNodesAndLabels(1);

            // complete the labelling for any nodes which only have a label for a single point
            LabelIsolatedNodes();

            // If a proper intersection was found, we can set a lower bound on the IM.
            ComputeProperIntersectionIM(intersector, im);

            /*
             * Now process improper intersections
             * (eg where one or other of the geometries has a vertex at the intersection point)
             * We need to compute the edge graph at all nodes to determine the IM.
             */

            // build EdgeEnds for all intersections
            EdgeEndBuilder eeBuilder = new EdgeEndBuilder();
            IList ee0 = eeBuilder.ComputeEdgeEnds(arg[0].GetEdgeEnumerator());
            InsertEdgeEnds(ee0);
            IList ee1 = eeBuilder.ComputeEdgeEnds(arg[1].GetEdgeEnumerator());
            InsertEdgeEnds(ee1);

            LabelNodeEdges();

            /*
             * Compute the labeling for isolated components
             * <br>
             * Isolated components are components that do not touch any other components in the graph.
             * They can be identified by the fact that they will
             * contain labels containing ONLY a single element, the one for their parent point.
             * We only need to check components contained in the input graphs, since
             * isolated components will not have been replaced by new components formed by intersections.
             */           
            LabelIsolatedEdges(0, 1);            
            LabelIsolatedEdges(1, 0);

            // update the IM from all components
            UpdateIM(im);
            return im;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ee"></param>
        private void InsertEdgeEnds(IList ee)
        {
            for (IEnumerator i = ee.GetEnumerator(); i.MoveNext(); )
            {
                EdgeEnd e = (EdgeEnd)i.Current;
                nodes.Add(e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="intersector"></param>
        /// <param name="im"></param>
        private void ComputeProperIntersectionIM(SegmentIntersector intersector, IntersectionMatrix im)
        {
            // If a proper intersection is found, we can set a lower bound on the IM.
            Dimensions dimA = arg[0].Geometry.Dimension;
            Dimensions dimB = arg[1].Geometry.Dimension;
            bool hasProper = intersector.HasProperIntersection;
            bool hasProperInterior = intersector.HasProperInteriorIntersection;

            // For Geometry's of dim 0 there can never be proper intersections.
            /*
             * If edge segments of Areas properly intersect, the areas must properly overlap.
             */
            if (dimA == Dimensions.Surface && dimB == Dimensions.Surface)
            {
                if (hasProper) 
                    im.SetAtLeast("212101212");
            }

            /*
             * If an Line segment properly intersects an edge segment of an Area,
             * it follows that the Interior of the Line intersects the Boundary of the Area.
             * If the intersection is a proper <i>interior</i> intersection, then
             * there is an Interior-Interior intersection too.
             * Note that it does not follow that the Interior of the Line intersects the Exterior
             * of the Area, since there may be another Area component which contains the rest of the Line.
             */
            else if (dimA == Dimensions.Surface && dimB == Dimensions.Curve)
            {
                if (hasProper) 
                    im.SetAtLeast("FFF0FFFF2");
                if (hasProperInterior) 
                    im.SetAtLeast("1FFFFF1FF");
            }

            else if (dimA == Dimensions.Curve && dimB == Dimensions.Surface)
            {
                if (hasProper) 
                    im.SetAtLeast("F0FFFFFF2");
                if (hasProperInterior)
                    im.SetAtLeast("1F1FFFFFF");
            }

            /* If edges of LineStrings properly intersect *in an interior point*, all
               we can deduce is that
               the interiors intersect.  (We can NOT deduce that the exteriors intersect,
               since some other segments in the geometries might cover the points in the
               neighbourhood of the intersection.)
               It is important that the point be known to be an interior point of
               both Geometries, since it is possible in a self-intersecting point to
               have a proper intersection on one segment that is also a boundary point of another segment.
            */
            else if (dimA == Dimensions.Curve && dimB == Dimensions.Curve)
            {
                if (hasProperInterior)
                    im.SetAtLeast("0FFFFFFFF");
            }
        }

        /// <summary>
        /// Copy all nodes from an arg point into this graph.
        /// The node label in the arg point overrides any previously computed
        /// label for that argIndex.
        /// (E.g. a node may be an intersection node with
        /// a computed label of Boundary,
        /// but in the original arg Geometry it is actually
        /// in the interior due to the Boundary Determination Rule)
        /// </summary>
        /// <param name="argIndex"></param>
        private void CopyNodesAndLabels(int argIndex)
        {
            for (IEnumerator i = arg[argIndex].GetNodeEnumerator(); i.MoveNext(); )
            {
                Node graphNode = (Node)i.Current;
                Node newNode = nodes.AddNode(graphNode.Coordinate);
                newNode.SetLabel(argIndex, graphNode.Label.GetLocation(argIndex));                
            }
        }

        /// <summary>
        /// Insert nodes for all intersections on the edges of a Geometry.
        /// Label the created nodes the same as the edge label if they do not already have a label.
        /// This allows nodes created by either self-intersections or
        /// mutual intersections to be labelled.
        /// Endpoint nodes will already be labelled from when they were inserted.
        /// </summary>
        /// <param name="argIndex"></param>
        private void ComputeIntersectionNodes(int argIndex)
        {
            for (IEnumerator i = arg[argIndex].GetEdgeEnumerator(); i.MoveNext(); )
            {
                Edge e = (Edge)i.Current;
                Locations eLoc = e.Label.GetLocation(argIndex);
                for (IEnumerator eiIt = e.EdgeIntersectionList.GetEnumerator(); eiIt.MoveNext(); )
                {
                    EdgeIntersection ei = (EdgeIntersection)eiIt.Current;
                    RelateNode n = (RelateNode)nodes.AddNode(ei.Coordinate);
                    if (eLoc == Locations.Boundary)
                        n.SetLabelBoundary(argIndex);
                    else
                    {
                        if (n.Label.IsNull(argIndex))
                            n.SetLabel(argIndex, Locations.Interior);
                    }
                }
            }
        }

        /// <summary>
        /// For all intersections on the edges of a Geometry,
        /// label the corresponding node IF it doesn't already have a label.
        /// This allows nodes created by either self-intersections or
        /// mutual intersections to be labelled.
        /// Endpoint nodes will already be labelled from when they were inserted.
        /// </summary>
        /// <param name="argIndex"></param>
        private void LabelIntersectionNodes(int argIndex)
        {
            for (IEnumerator i = arg[argIndex].GetEdgeEnumerator(); i.MoveNext(); )
            {
                Edge e = (Edge)i.Current;
                Locations eLoc = e.Label.GetLocation(argIndex);
                for (IEnumerator eiIt = e.EdgeIntersectionList.GetEnumerator(); eiIt.MoveNext(); )
                {
                    EdgeIntersection ei = (EdgeIntersection)eiIt.Current;
                    RelateNode n = (RelateNode)nodes.Find(ei.Coordinate);
                    if (n.Label.IsNull(argIndex))
                    {
                        if (eLoc == Locations.Boundary)
                             n.SetLabelBoundary(argIndex);
                        else n.SetLabel(argIndex, Locations.Interior);
                    }
                }
            }
        }

        /// <summary>
        /// If the Geometries are disjoint, we need to enter their dimension and
        /// boundary dimension in the Ext rows in the IM
        /// </summary>
        /// <param name="im"></param>
        private void ComputeDisjointIM(IntersectionMatrix im)
        {
            IGeometry ga = arg[0].Geometry;
            if (!ga.IsEmpty)
            {
                im.Set(Locations.Interior, Locations.Exterior, ga.Dimension);
                im.Set(Locations.Boundary, Locations.Exterior, ga.BoundaryDimension);
            }
            IGeometry gb = arg[1].Geometry;
            if (!gb.IsEmpty)
            {
                im.Set(Locations.Exterior, Locations.Interior, gb.Dimension);
                im.Set(Locations.Exterior, Locations.Boundary, gb.BoundaryDimension);    
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void LabelNodeEdges()
        {
            for (IEnumerator ni = nodes.GetEnumerator(); ni.MoveNext(); )
            {
                RelateNode node = (RelateNode)ni.Current;
                node.Edges.ComputeLabelling(arg);                
            }
        }

        /// <summary>
        /// Update the IM with the sum of the IMs for each component.
        /// </summary>
        /// <param name="im"></param>
        private void UpdateIM(IntersectionMatrix im)
        {
            for (IEnumerator ei = isolatedEdges.GetEnumerator(); ei.MoveNext(); )
            {
                Edge e = (Edge)ei.Current;
                e.UpdateIM(im);
            }
            for (IEnumerator ni = nodes.GetEnumerator(); ni.MoveNext(); )
            {
                RelateNode node = (RelateNode)ni.Current;
                node.UpdateIM(im);
                node.UpdateIMFromEdges(im);
            }
        }

        /// <summary> 
        /// Processes isolated edges by computing their labelling and adding them
        /// to the isolated edges list.
        /// Isolated edges are guaranteed not to touch the boundary of the target (since if they
        /// did, they would have caused an intersection to be computed and hence would
        /// not be isolated).
        /// </summary>
        /// <param name="thisIndex"></param>
        /// <param name="targetIndex"></param>
        private void LabelIsolatedEdges(int thisIndex, int targetIndex)
        {
            for (IEnumerator ei = arg[thisIndex].GetEdgeEnumerator(); ei.MoveNext(); )
            {
                Edge e = (Edge)ei.Current;
                if (e.IsIsolated)
                {
                    LabelIsolatedEdge(e, targetIndex, arg[targetIndex].Geometry);
                    isolatedEdges.Add(e);
                }
            }
        }

        /// <summary>
        /// Label an isolated edge of a graph with its relationship to the target point.
        /// If the target has dim 2 or 1, the edge can either be in the interior or the exterior.
        /// If the target has dim 0, the edge must be in the exterior.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="targetIndex"></param>
        /// <param name="target"></param>
        private void LabelIsolatedEdge(Edge e, int targetIndex, IGeometry target)
        {
            // this won't work for GeometryCollections with both dim 2 and 1 geoms
            if (target.Dimension > 0)
            {
                // since edge is not in boundary, may not need the full generality of PointLocator?
                // Possibly should use ptInArea locator instead?  We probably know here
                // that the edge does not touch the bdy of the target Geometry
                Locations loc = ptLocator.Locate(e.Coordinate, target);
                e.Label.SetAllLocations(targetIndex, loc);
            }
            else e.Label.SetAllLocations(targetIndex, Locations.Exterior);            
        }

        /// <summary>
        /// Isolated nodes are nodes whose labels are incomplete
        /// (e.g. the location for one Geometry is null).
        /// This is the case because nodes in one graph which don't intersect
        /// nodes in the other are not completely labelled by the initial process
        /// of adding nodes to the nodeList.
        /// To complete the labelling we need to check for nodes that lie in the
        /// interior of edges, and in the interior of areas.
        /// </summary>
        private void LabelIsolatedNodes()
        {
            for (IEnumerator ni = nodes.GetEnumerator(); ni.MoveNext(); )
            {
                Node n = (Node) ni.Current;
                Label label = n.Label;
                // isolated nodes should always have at least one point in their label
                Assert.IsTrue(label.GeometryCount > 0, "node with empty label found");
                if (n.IsIsolated)
                {
                    if (label.IsNull(0))
                         LabelIsolatedNode(n, 0);
                    else LabelIsolatedNode(n, 1);
                }
            }
        }

        /// <summary>
        /// Label an isolated node with its relationship to the target point.
        /// </summary>
        /// <param name="n"></param>
        /// <param name="targetIndex"></param>
        private void LabelIsolatedNode(Node n, int targetIndex)
        {
            Locations loc = ptLocator.Locate(n.Coordinate, arg[targetIndex].Geometry);
            n.Label.SetAllLocations(targetIndex, loc);        
        }
    }
}
