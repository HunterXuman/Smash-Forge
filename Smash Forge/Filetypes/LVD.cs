﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Drawing;
using System.Diagnostics;



namespace Smash_Forge
{
    public abstract class LVDEntry
    {
        public abstract string magic { get; }
        public string name = "";
        public string subname = "";
        public Vector3 startPos = new Vector3();
        public bool useStartPos = false;
        public int unk1 = 0;
        public float[] unk2 = new float[3];
        public int unk3 = unchecked((int)0xFFFFFFFF);
        public char[] boneName = new char[0x40];

        public string BoneName
        {
            get
            {
                string name = "";
                foreach (char b in boneName)
                    if (b != (char)0)
                        name += b;
                    else break;
                return name;
            }
        }

        public void read(FileData f)
        {
            f.skip(0xC);

            f.skip(1);
            name = f.readString(f.pos(), 0x38);
            f.skip(0x38);

            f.skip(1);
            subname = f.readString(f.pos(), 0x40);
            f.skip(0x40);

            f.skip(1);
            for (int i = 0; i < 3; i++)
                startPos[i] = f.readFloat();
            useStartPos = Convert.ToBoolean(f.readByte());

            f.skip(1);
            unk1 = f.readInt(); //Some kind of count? Only seen it as 0 so I don't know what it's for

            //Not sure what this is for, but it seems like it could be an x,y,z followed by a hash
            f.skip(1);
            for (int i = 0; i < 3; i++)
                unk2[i] = f.readFloat();
            unk3 = f.readInt();

            f.skip(1);
            boneName = new char[0x40];
            for (int i = 0; i < 0x40; i++)
                boneName[i] = (char)f.readByte();
        }
        public void save(FileOutput f)
        {
            f.writeHex(magic);

            f.writeByte(1);
            f.writeString(name.PadRight(0x38, (char)0));

            f.writeByte(1);
            f.writeString(subname.PadRight(0x40, (char)0));

            f.writeByte(1);
            for (int i = 0; i < 3; i++)
                f.writeFloat(startPos[i]);
            f.writeFlag(useStartPos);

            f.writeByte(1);
            f.writeInt(unk1);

            f.writeByte(1);
            foreach (float i in unk2)
                f.writeFloat(i);
            f.writeInt(unk3);

            f.writeByte(1);
            f.writeChars(boneName);
        }
    }

    public class Vector2D
    {
        public float x;
        public float y;

        public Vector2D() {}
        public Vector2D(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }
    
    public class Point : LVDEntry
    {
        public override string magic { get { return ""; } }
        public float x;
        public float y;
    }
    
    public class CollisionMat
    {
        //public bool leftLedge;
        //public bool rightLedge;
        //public bool noWallJump;
        //public byte physicsType;
        public byte[] material = new byte[0xC];

        public bool getFlag(int n)
        {
            return ((material[10] & (1 << n)) != 0);
        }

        public byte getPhysics()
        {
            return material[3];
        }

        public void setFlag(int flag, bool value)
        {
            //Console.WriteLine("B - " + getFlag(flag));
            byte mask = (byte)(1 << flag);
            bool isSet = (material[10] & mask) != 0;
            if(value)
                material[10] |= mask;
            else
                material[10] &= (byte)~mask;
            //Console.WriteLine("A - " + getFlag(flag));
        }

        public void setPhysics(byte b)
        {
            material[3] = b;
        }
    }

    public class CollisionCliff : LVDEntry
    {
        public override string magic { get { return "030401017735BB7500000002"; } }

        public Vector2D pos;
        public float angle; //I don't know what this does exactly, but it's -1 for left and 1 for right
        public int lineIndex;

        public void read(FileData f)
        {
            base.read(f);

            f.skip(1);
            pos = new Vector2D();
            pos.x = f.readFloat();
            pos.y = f.readFloat();
            angle = f.readFloat();
            lineIndex = f.readInt();
        }
        public void save(FileOutput f)
        {
            base.save(f);

            f.writeByte(1);
            f.writeFloat(pos.x);
            f.writeFloat(pos.y);
            f.writeFloat(angle);
            f.writeInt(lineIndex);
        }
    }
    
    public class Collision : LVDEntry
    {
        public override string magic { get { return "030401017735BB7500000002"; } }

        public List<Vector2D> verts = new List<Vector2D>();
        public List<Vector2D> normals = new List<Vector2D>();
        public List<CollisionCliff> cliffs = new List<CollisionCliff>();
        public List<CollisionMat> materials = new List<CollisionMat>();
        //Flags: ???, rig collision, ???, drop-through
        public bool flag1 = false, flag2 = false, flag3 = false, flag4 = false;

        public Collision() {}

        public void read(FileData f)
        {
            base.read(f);

            flag1 = Convert.ToBoolean(f.readByte());
            flag2 = Convert.ToBoolean(f.readByte());
            flag3 = Convert.ToBoolean(f.readByte());
            flag4 = Convert.ToBoolean(f.readByte());

            f.skip(1);
            int vertCount = f.readInt();
            for(int i = 0; i < vertCount; i++)
            {
                f.skip(1);
                Vector2D temp = new Vector2D();
                temp.x = f.readFloat();
                temp.y = f.readFloat();
                verts.Add(temp);
            }

            f.skip(1);
            int normalCount = f.readInt();
            for(int i = 0; i < normalCount; i++)
            {
                f.skip(1);
                Vector2D temp = new Vector2D();
                temp.x = f.readFloat();
                temp.y = f.readFloat();
                normals.Add(temp);
            }

            f.skip(1);
            int cliffCount = f.readInt();
            for(int i = 0; i < cliffCount; i++)
            {
                CollisionCliff temp = new CollisionCliff();
                temp.read(f);
                cliffs.Add(temp);
            }

            f.skip(1);
            int materialCount = f.readInt();
            for(int i = 0; i < materialCount; i++)
            {
                f.skip(1);
                CollisionMat temp = new CollisionMat();
                temp.material = f.read(0xC);//Temporary, will work on fleshing out material more later
                materials.Add(temp);
            }

        }
        public void save(FileOutput f)
        {
            base.save(f);

            f.writeFlag(flag1);
            f.writeFlag(flag2);
            f.writeFlag(flag3);
            f.writeFlag(flag4);

            f.writeByte(1);
            f.writeInt(verts.Count);
            foreach(Vector2D v in verts)
            {
                f.writeByte(1);
                f.writeFloat(v.x);
                f.writeFloat(v.y);
            }

            f.writeByte(1);
            f.writeInt(normals.Count);
            foreach (Vector2D n in normals)
            {
                f.writeByte(1);
                f.writeFloat(n.x);
                f.writeFloat(n.y);
            }

            f.writeByte(1);
            f.writeInt(cliffs.Count);
            foreach (CollisionCliff c in cliffs)
            {
                c.save(f);
            }

            f.writeByte(1);
            f.writeInt(materials.Count);
            foreach (CollisionMat m in materials)
            {
                f.writeByte(1);
                f.writeBytes(m.material);
            }
        }
    }

    public class Spawn : LVDEntry
    {
        public override string magic { get { return "020401017735BB7500000002"; } }

        public float x;
        public float y;

        public void read(FileData f)
        {
            base.read(f);

            f.skip(1);
            x = f.readFloat();
            y = f.readFloat();
        }
        public void save(FileOutput f)
        {
            base.save(f);

            f.writeByte(1);
            f.writeFloat(x);
            f.writeFloat(y);
        }
    }

    public class Bounds : LVDEntry //For Camera Bounds and Blast Zones
    {
        public override string magic { get { return "020401017735BB7500000002"; } }

        public float top;
        public float bottom;
        public float left;
        public float right;

        public void read(FileData f)
        {
            base.read(f);

            f.skip(1);
            left = f.readFloat();
            right = f.readFloat();
            top = f.readFloat();
            bottom = f.readFloat();
        }
        public void save(FileOutput f)
        {
            base.save(f);

            f.writeByte(1);
            f.writeFloat(left);
            f.writeFloat(right);
            f.writeFloat(top);
            f.writeFloat(bottom);
        }
    }

    public class Section
    {
        public List<Vector2D> points = new List<Vector2D>();

        public void read(FileData f)
        {
            f.skip(1);
            f.skip(0x16);// unknown data

            f.skip(1);
            points = new List<Vector2D>();
            int vertCount = f.readInt();
            for(int j = 0; j < vertCount; j++)
            {
                f.skip(1);
                Vector2D point = new Vector2D();
                point.x = f.readFloat();
                point.y = f.readFloat();
                points.Add(point);
            }
        }
        public void save(FileOutput f)
        {
            f.writeByte(1);
            f.writeHex("03000000040000000000000000000000000000000001");

            f.writeByte(1);
            f.writeInt(points.Count);
            foreach(Vector2D v in points)
            {
                f.writeByte(1);
                f.writeFloat(v.x);
                f.writeFloat(v.y);
            }
        }
    }


    public class ItemSpawner : LVDEntry
    {
        public override string magic { get { return "010401017735BB7500000002"; } }

        public int id;
        public List<Section> sections = new List<Section>();

        public ItemSpawner() {}

        public void read(FileData f)
        {
            base.read(f);

            f.skip(1);
            id = f.readInt();

            f.skip(1);
            f.skip(1);
            int sectionCount = f.readInt();
            for(int i = 0; i < sectionCount; i++)
            {
                Section temp = new Section();
                temp.read(f);
                sections.Add(temp);
            }
        }
        public void save(FileOutput f)
        {
            base.save(f);

            f.writeByte(1);
            f.writeInt(id);

            f.writeByte(1);
            f.writeByte(1);
            f.writeInt(sections.Count);
            foreach(Section s in sections)
            {
                s.save(f);
            }
        }
    }
    
    public class EnmSection
    {
        public int type;
        public float x,y,z,unk;

        public void read(FileData f)
        {
            f.skip(0x2); //x01 03

            type = f.readInt(); //First set of sections = type 1, second set = type 3. No difference in structure but type 3 seems to have non-zero for z and unk
            x = f.readFloat();
            y = f.readFloat();
            z = f.readFloat(); //0 unless type 3
            unk = f.readFloat(); //0 unless type 3

            f.skip(0x2); //x01 01
            f.readInt(); //Only seen this as 0, probably a count
        }
        public void save(FileOutput f)
        {
            f.writeHex("0103");

            f.writeInt(type);
            f.writeFloat(x);
            f.writeFloat(y);
            f.writeFloat(z);
            f.writeFloat(unk);

            f.writeHex("0101");
            f.writeInt(0);
        }
    }

    public class EnemyGenerator : LVDEntry
    {
        public override string magic { get { return "030401017735BB7500000002"; } }

        public int id;
        public List<EnmSection> sections = new List<EnmSection>();
        public List<EnmSection> sections2 = new List<EnmSection>();
        public List<int> ids = new List<int>();
        public int padCount = 0;

        public void read(FileData f)
        {
            base.read(f);

            f.skip(0x2); //x01 01
            int sectionCount = f.readInt();
            for (int i = 0; i < sectionCount; i++)
            {
                EnmSection temp = new EnmSection();
                temp.read(f);
                sections.Add(temp);
            }

            f.skip(0x2); //x01 01
            int sectionCount2 = f.readInt();
            for (int i = 0; i < sectionCount2; i++)
            {
                EnmSection temp = new EnmSection();
                temp.read(f);
                sections2.Add(temp);
            }

            f.skip(0x2); //x01 01
            int unkCount = f.readInt();
            for (int i = 0; i < unkCount; i++)
            {
                //Only seen this count as 0
            }

            f.skip(1); //x01
            id = f.readInt();

            f.skip(1); //x01
            int idCount = f.readInt();
            for (int i = 0; i < idCount; i++)
            {
                f.skip(1);
                ids.Add(f.readInt());
            }

            f.skip(1); //x01
            f.readInt(); //Only seen as 0
            f.skip(1); //x01
            padCount = f.readInt(); //Don't know the purpose of this, it just seems to be 1 if there's the extra 5 bytes thrown on the end
            for (int i = 0; i < padCount; i++)
                f.skip(0x5); //x01 00 00 00 00
        }
        public void save(FileOutput f)
        {
            base.save(f);

            f.writeHex("0101");
            f.writeInt(sections.Count);
            foreach (EnmSection temp in sections)
                temp.save(f);

            f.writeHex("0101");
            f.writeInt(sections2.Count);
            foreach (EnmSection temp in sections2)
                temp.save(f);

            f.writeHex("0101");
            f.writeInt(0);

            f.writeByte(1);
            f.writeInt(id);

            f.writeByte(1);
            f.writeInt(ids.Count);
            foreach (int temp in ids)
            {
                f.writeByte(1);
                f.writeInt(temp);
            }

            f.writeByte(1);
            f.writeInt(0);
            f.writeByte(1);
            f.writeInt(padCount);
            for (int i = 0; i < padCount; i++)
                f.writeHex("0100000000");
        }
    }

    public enum shape
    {
        point = 1,
        rectangle = 3,
        path = 4
    }

    public class LVDShape : LVDEntry
    {
        public override string magic { get { return "010401017735BB7500000002"; } }
        
        public int type; 
    }
    
    public class LVDGeneralShape : LVDShape
    {
        public int id;

        public void read(FileData f)
        {
            base.read(f);
            
            f.skip(1);
            id = f.readInt();
        }
        public void save(FileOutput f)
        {
            base.save(f);
            
            f.writeByte(1);
            f.writeInt(id);
        }
    }

    //All objects in the general shapes section have this same structure
    public class GeneralShape : LVDGeneralShape
    {
        public float x1, y1, x2, y2;
        public List<Vector2D> points = new List<Vector2D>();

        public void read(FileData f)
        {
            base.read(f);

            f.readByte();
            type = f.readInt(); //1 = point, 3 = rect, 4 = path
            if ((type != 1) && (type != 3) && (type != 4))
                throw new NotImplementedException($"Unknown general shape type {type} at offset {f.pos()-4}");

            x1 = f.readFloat();
            y1 = f.readFloat();
            x2 = f.readFloat();
            y2 = f.readFloat();

            f.skip(1);
            f.skip(1);
            int pointCount = f.readInt();
            for(int i = 0; i < pointCount; i++)
            {
                f.skip(1);
                points.Add(new Vector2D() { x = f.readFloat(), y = f.readFloat() });
            }
        }
        public void save(FileOutput f)
        {
            base.save(f);

            f.writeByte(0x3);
            f.writeInt(type);

            f.writeFloat(x1);
            f.writeFloat(y1);
            f.writeFloat(x2);
            f.writeFloat(y2);

            f.writeByte(1);
            f.writeByte(1);
            f.writeInt(points.Count);
            foreach(Vector2D point in points)
            {
                f.writeByte(1);
                f.writeFloat(point.x);
                f.writeFloat(point.y);
            }
        }
    }
    
    public class GeneralPoint : LVDGeneralShape
    {
        public float x, y, z;

        public GeneralPoint()
        {
            type = 4;
        }

        public void read(FileData f)
        {
            base.read(f);

            f.skip(1);
            type = f.readInt(); //always 4?

            x = f.readFloat();
            y = f.readFloat();
            z = f.readFloat();
            f.skip(0x10);
        }
        public void save(FileOutput f)
        {
            base.save(f);

            f.writeByte(1);
            f.writeInt(type);

            f.writeFloat(x);
            f.writeFloat(y);
            f.writeFloat(z);
            f.writeHex("00000000000000000000000000000000");
        }
    }
    
    public class DamageShape : LVDShape
    {
        public float x;
        public float y;
        public float z;
        public float dx;
        public float dy;
        public float dz;
        public float radius;
        public float unk;

        public void read(FileData f)
        {
            base.read(f);
            
            f.skip(1);
            type = f.readInt(); //2 = sphere, 3 = capsule
            if ((type != 2) && (type != 3))
                throw new NotImplementedException($"Unknown damage shape type {type} at offset {f.pos()-4}");
            
            x = f.readFloat();
            y = f.readFloat();
            z = f.readFloat();
            if (type == 2)
            {
                radius = f.readFloat();
                dx = f.readFloat();
                dy = f.readFloat();
                dz = f.readFloat();
            }
            else if (type == 3)
            {
                dx = f.readFloat();
                dy = f.readFloat();
                dz = f.readFloat();
                radius = f.readFloat();
            }
            unk = f.readFloat();
            f.skip(0x1);
        }
        public void save(FileOutput f)
        {
            base.save(f);

            f.writeByte(1);
            f.writeInt(type);

            f.writeFloat(x);
            f.writeFloat(y);
            f.writeFloat(z);
            if (type == 2)
            {
                f.writeFloat(radius);
                f.writeFloat(dx);
                f.writeFloat(dy);
                f.writeFloat(dz);
            }
            else if (type == 3)
            {
                f.writeFloat(dx);
                f.writeFloat(dy);
                f.writeFloat(dz);
                f.writeFloat(radius);
            }
            f.writeFloat(unk);
            f.writeHex("00");
        }
    }

    public class LVD : FileBase
    { 
        public LVD()
        {
            collisions = new List<Collision>();
            spawns = new List<Spawn>();
            respawns = new List<Spawn>();
            cameraBounds = new List<Bounds>();
            blastzones = new List<Bounds>();
            enemySpawns = new List<EnemyGenerator>();
            damageShapes = new List<DamageShape>();
            itemSpawns = new List<ItemSpawner>();
            generalShapes = new List<GeneralShape>();
            generalPoints = new List<GeneralPoint>();
        }
        public LVD(string filename) : this()
        {
            Read(filename);
        }
        public List<Collision> collisions { get; set; }
        public List<Spawn> spawns { get; set; }
        public List<Spawn> respawns { get; set; }
        public List<Bounds> cameraBounds { get; set; }
        public List<Bounds> blastzones { get; set; }
        public List<EnemyGenerator> enemySpawns { get; set; }
        public List<DamageShape> damageShapes { get; set; }
        public List<ItemSpawner> itemSpawns { get; set; }
        public List<GeneralShape> generalShapes { get; set; }
        public List<GeneralPoint> generalPoints { get; set; }

        public override Endianness Endian { get; set; }

        /*type 1  - collisions
          type 2  - spawns
          type 3  - respawns
          type 4  - camera bounds
          type 5  - death boundaries
          type 6  - enemy generator
          type 7  - ITEMPT_transform
          type 8  - ???
          type 9  - ITEMPT
          type 10 - fsAreaCam (and other fsArea's ? )
          type 11 - fsCamLimit
          type 12 - damageShapes (damage sphere and damage capsule are the only ones I've seen, type 2 and 3 respectively)
          type 13 - item spawners
          type 14 - general shapes (general rect, general path, etc.)
          type 15 - general points
          type 16 - ???
          type 17 - FsStartPoint
          type 18 - ???
          type 19 - ???*/

        public override void Read(string filename)
        {
            FileData f = new FileData(filename);
            f.skip(0xA);//It's magic

            f.skip(1);
            int collisionCount = f.readInt();
            for (int i = 0; i < collisionCount; i++)
            {
                Collision temp = new Collision();
                temp.read(f);
                collisions.Add(temp);
            }

            f.skip(1);
            int spawnCount = f.readInt();
            for (int i = 0; i < spawnCount; i++)
            {
                Spawn temp = new Spawn();
                temp.read(f);
                spawns.Add(temp);
            }

            f.skip(1);
            int respawnCount = f.readInt();
            for (int i = 0; i < respawnCount; i++)
            {
                Spawn temp = new Spawn();
                temp.read(f);
                respawns.Add(temp);
            }

            f.skip(1);
            int cameraCount = f.readInt();
            for (int i = 0; i < cameraCount; i++)
            {
                Bounds temp = new Bounds();
                temp.read(f);
                cameraBounds.Add(temp);
            }

            f.skip(1);
            int blastzoneCount = f.readInt();
            for (int i = 0; i < blastzoneCount; i++)
            {
                Bounds temp = new Bounds();
                temp.read(f);
                blastzones.Add(temp);
            }

            f.skip(1);
            int enemyGeneratorCount = f.readInt();
            for (int i = 0; i < enemyGeneratorCount; i++)
            {
                EnemyGenerator temp = new EnemyGenerator();
                temp.read(f);
                enemySpawns.Add(temp);
            }

            f.skip(1);
            if (f.readInt() != 0) //7
                return;

            f.skip(1);
            if (f.readInt() != 0) //8
                return;

            f.skip(1);
            if (f.readInt() != 0) //9
                return;
            
            f.skip(1);
            int fsAreaCamCount = f.readInt();
            if (fsAreaCamCount != 0)
                return;
            
            f.skip(1);
            int fsCamLimitCount = f.readInt();
            if (fsCamLimitCount != 0)
                return;
            
            f.skip(1);
            int damageShapeCount = f.readInt();
            for(int i=0; i < damageShapeCount; i++)
            {
                DamageShape temp = new DamageShape();
                temp.read(f);
                damageShapes.Add(temp);
            }
            
            f.skip(1);
            int itemCount = f.readInt();
            for(int i = 0; i < itemCount; i++)
            {
                ItemSpawner temp = new ItemSpawner();
                temp.read(f);
                itemSpawns.Add(temp);
            }
            
            f.skip(1);
            int generalShapeCount = f.readInt();
            for (int i = 0; i < generalShapeCount; i++)
            {
                GeneralShape temp = new GeneralShape();
                temp.read(f);
                generalShapes.Add(temp);
            }
            
            f.skip(1);
            int generalPointCount = f.readInt();
            for(int i = 0; i < generalPointCount; i++)
            {
                GeneralPoint temp = new GeneralPoint();
                temp.read(f);
                generalPoints.Add(temp);
            }

            f.skip(1);
            if (f.readInt() != 0) //16
                return; //no clue how to be consistent in reading these so...
                
            f.skip(1);
            if (f.readInt() != 0) //17
                return; //no clue how to be consistent in reading these so...
                
            f.skip(1);
            if (f.readInt() != 0) //18
                return; //no clue how to be consistent in reading these so...
            
            f.skip(1);
            if (f.readInt() != 0) //19
                return; //no clue how to be consistent in reading these so...

            //LVD doesn't end here and neither does my confusion, will update this part later
        }

        public override byte[] Rebuild()
        {
            FileOutput f = new FileOutput();
            f.Endian = Endianness.Big;

            f.writeHex("000000010A014C564431");

            f.writeByte(1);
            f.writeInt(collisions.Count);
            foreach (Collision c in collisions)
                c.save(f);

            f.writeByte(1);
            f.writeInt(spawns.Count);
            foreach (Spawn s in spawns)
                s.save(f);

            f.writeByte(1);
            f.writeInt(respawns.Count);
            foreach (Spawn s in respawns)
                s.save(f);

            f.writeByte(1);
            f.writeInt(cameraBounds.Count);
            foreach (Bounds b in cameraBounds)
                b.save(f);

            f.writeByte(1);
            f.writeInt(blastzones.Count);
            foreach (Bounds b in blastzones)
                b.save(f);

            f.writeByte(1);
            f.writeInt(enemySpawns.Count);
            foreach (EnemyGenerator e in enemySpawns)
                e.save(f);

            for (int i = 0; i < 5; i++)
            {
                f.writeByte(1);
                f.writeInt(0);
            }

            f.writeByte(1);
            f.writeInt(damageShapes.Count);
            foreach (DamageShape shape in damageShapes)
                shape.save(f);
            
            f.writeByte(1);
            f.writeInt(itemSpawns.Count);
            foreach (ItemSpawner item in itemSpawns)
                item.save(f);

            f.writeByte(1);
            f.writeInt(generalShapes.Count);
            foreach (GeneralShape shape in generalShapes)
                shape.save(f);
            
            f.writeByte(1);
            f.writeInt(generalPoints.Count);
            foreach (GeneralPoint p in generalPoints)
                p.save(f);

            for (int i = 0; i < 4; i++)
            {
                f.writeByte(1);
                f.writeInt(0);
            }

            return f.getBytes();
        }

        //Function to automatically add a cliff to every grabbable ledge in a given collision
        //Works just like the vanilla game would have it
        public void GenerateCliffs(Collision col)
        {
            int[] counts = new int[2];
            bool[,] lines = new bool[col.materials.Count,2];
            for (int i = 0; i < col.materials.Count; i++)
            {
                lines[i,0] = col.materials[i].getFlag(6);
                lines[i,1] = col.materials[i].getFlag(7);
                if (lines[i,0]) counts[0]++;
                if (lines[i,1]) counts[1]++;
            }

            string nameSub;
            if (col.name.Length > 4 && col.name.StartsWith("COL_"))
                nameSub = col.name.Substring(4, col.name.Length - 4);
            else
                nameSub = "Collision";

            col.cliffs = new List<CollisionCliff>();
            counts[0] = counts[0] > 1 ? 1 : 0;
            counts[1] = counts[1] > 1 ? 1 : 0;
            for (int i = 0; i < col.materials.Count; i++)
            {
                if (lines[i,0])
                {
                    string cliffName = "CLIFF_" + nameSub + "L" + (counts[0] > 0 ? $"{counts[0]++}" : "");
                    CollisionCliff temp = new CollisionCliff();
                    temp.name = cliffName;
                    temp.subname = cliffName.Substring(6, cliffName.Length - 6);
                    temp.startPos = new Vector3(col.verts[i].x, col.verts[i].y, 0);
                    temp.pos = new Vector2D(col.verts[i].x, col.verts[i].y);
                    temp.angle = -1.0f;
                    temp.lineIndex = i;
                    col.cliffs.Add(temp);
                }
                if (lines[i,1])
                {
                    string cliffName = "CLIFF_" + nameSub + "R" + (counts[1] > 0 ? $"{counts[1]++}" : "");
                    CollisionCliff temp = new CollisionCliff();
                    temp.name = cliffName;
                    temp.subname = cliffName.Substring(6, cliffName.Length - 6);
                    temp.startPos = new Vector3(col.verts[i+1].x, col.verts[i+1].y, 0);
                    temp.pos = new Vector2D(col.verts[i+1].x, col.verts[i+1].y);
                    temp.angle = 1.0f;
                    temp.lineIndex = i;
                    col.cliffs.Add(temp);
                }
            }
        }

        #region rendering

        public object LVDSelection;
        public MeshList MeshList;

        public void Render()
        {
            GL.Disable(EnableCap.CullFace);

            /*foreach (ModelContainer m in ModelContainers)
            {

                if (m.dat_melee != null && m.dat_melee.collisions != null)
                {
                    LVD.DrawDATCollisions(m);

                }

                if (m.dat_melee != null && m.dat_melee.blastzones != null)
                {
                    LVD.DrawBounds(m.dat_melee.blastzones, Color.Red);
                }

                if (m.dat_melee != null && m.dat_melee.cameraBounds != null)
                {
                    LVD.DrawBounds(m.dat_melee.cameraBounds, Color.Blue);
                }

                if (m.dat_melee != null && m.dat_melee.targets != null)
                {
                    foreach (Point target in m.dat_melee.targets)
                    {
                        RenderTools.drawCircleOutline(new Vector3(target.x, target.y, 0), 2, 30);
                        RenderTools.drawCircleOutline(new Vector3(target.x, target.y, 0), 4, 30);
                    }
                }

                if (m.dat_melee != null && m.dat_melee.respawns != null)
                    foreach (Point r in m.dat_melee.respawns)
                    {
                        Spawn temp = new Spawn() { x = r.x, y = r.y };
                        LVD.DrawSpawn(temp, true);
                    }

                if (m.dat_melee != null && m.dat_melee.spawns != null)
                    foreach (Point r in m.dat_melee.spawns)
                    {
                        Spawn temp = new Spawn() { x = r.x, y = r.y };
                        LVD.DrawSpawn(temp, false);
                    }

                GL.Color4(Color.FromArgb(200, Color.Fuchsia));
                if (m.dat_melee != null && m.dat_melee.itemSpawns != null)
                    foreach (Point r in m.dat_melee.itemSpawns)
                        RenderTools.drawCubeWireframe(new Vector3(r.x, r.y, 0), 3);
            }
            */
            //if (Runtime.TargetLVD != null)
            {
                if (Runtime.renderCollisions)
                {
                    DrawCollisions();
                }

                if (Runtime.renderItemSpawners)
                {
                    DrawItemSpawners();
                }

                if (Runtime.renderSpawns)
                {
                    foreach (Spawn s in spawns)
                        DrawSpawn(s, false);
                }

                if (Runtime.renderRespawns)
                {
                    foreach (Spawn s in respawns)
                        DrawSpawn(s, true);
                }

                if (Runtime.renderGeneralPoints)
                {
                    foreach (GeneralPoint p in generalPoints)
                        DrawPoint(p);

                    foreach (GeneralShape s in generalShapes)
                        DrawShape(s);
                }

                if (Runtime.renderOtherLVDEntries)
                {
                    DrawEnemySpawners();

                    foreach (DamageShape s in damageShapes)
                        DrawShape(s);

                    foreach (Bounds b in cameraBounds)
                        DrawBounds(b, Color.Blue);

                    foreach (Bounds b in blastzones)
                        DrawBounds(b, Color.Red);
                }
            }

            GL.Enable(EnableCap.CullFace);
        }

        public static void DrawPoint(GeneralPoint p)
        {
            GL.LineWidth(2);

            Vector3 pos = p.useStartPos ? p.startPos : new Vector3(p.x,p.y,p.z);

            GL.Color3(Color.Red);
            RenderTools.drawCubeWireframe(pos, 3);
        }

        public static void DrawShape(GeneralShape s)
        {
            GL.LineWidth(2);
            GL.Color4(Color.FromArgb(200, Color.Fuchsia));

            Vector3 sPos = s.useStartPos ? s.startPos : new Vector3(0,0,0);

            if(s.type == 1)
            {
                Vector3 pos = s.useStartPos ? sPos : new Vector3(s.x1,s.y1,0);
                RenderTools.drawCubeWireframe(pos, 3);
            }
            if(s.type == 3)
            {
                GL.Begin(PrimitiveType.LineLoop);
                GL.Vertex3(s.x1+sPos[0], s.y1+sPos[1], 0+sPos[2]);
                GL.Vertex3(s.x2+sPos[0], s.y1+sPos[1], 0+sPos[2]);
                GL.Vertex3(s.x2+sPos[0], s.y2+sPos[1], 0+sPos[2]);
                GL.Vertex3(s.x1+sPos[0], s.y2+sPos[1], 0+sPos[2]);
            }
            if(s.type == 4)
            {
                GL.Begin(PrimitiveType.LineStrip);
                foreach(Vector2D point in s.points)
                    GL.Vertex3(point.x+sPos[0], point.y+sPos[1], 0+sPos[2]);
            }

            GL.End();
        }

        public static void DrawShape(DamageShape s)
        {
            GL.LineWidth(2);
            GL.Color4(Color.FromArgb(128, Color.Yellow));

            Vector3 sPos = s.useStartPos ? s.startPos : new Vector3(0,0,0);
            Vector3 pos = new Vector3(s.x, s.y, s.z);
            Vector3 posd = new Vector3(s.dx, s.dy, s.dz);

            if (s.type == 2)
                RenderTools.drawSphere(sPos+pos, s.radius, 24);
            if (s.type == 3)
                RenderTools.drawCylinder(sPos+pos, sPos+pos+posd, s.radius);
        }

        public static void DrawSpawn(Spawn s, bool isRespawn)
        {
            GL.LineWidth(2);

            Vector3 pos = s.useStartPos ? s.startPos : new Vector3(s.x,s.y,0);
            float x = pos[0], y = pos[1], z = pos[2];

            //Draw quad
            float width = 3.0f;
            float height = 10.0f;

            GL.Color4(Color.FromArgb(100, Color.Blue));
            GL.Begin(PrimitiveType.QuadStrip);

            GL.Vertex3(x - width, y, z);
            GL.Vertex3(x + width, y, z);
            GL.Vertex3(x - width, y + height, z);
            GL.Vertex3(x + width, y + height, z);

            GL.End();

            //Respawn platform
            if (isRespawn)
            {
                float scale = 5.0f;

                //Draw arrow
                GL.Color4(Color.FromArgb(200, Color.Gray));
                GL.Begin(PrimitiveType.Triangles);

                GL.Vertex3(x - scale, y, z);
                GL.Vertex3(x + scale, y, z);
                GL.Vertex3(x, y, z + scale);

                GL.Vertex3(x - scale, y, z);
                GL.Vertex3(x + scale, y, z);
                GL.Vertex3(x, y, z - scale);

                GL.Vertex3(x - scale, y, z);
                GL.Vertex3(x, y - scale, z);
                GL.Vertex3(x, y, z + scale);

                GL.Vertex3(x + scale, y, z);
                GL.Vertex3(x, y - scale, z);
                GL.Vertex3(x, y, z - scale);

                GL.Vertex3(x + scale, y, z);
                GL.Vertex3(x, y - scale, z);
                GL.Vertex3(x, y, z + scale);

                GL.Vertex3(x - scale, y, z);
                GL.Vertex3(x, y - scale, z);
                GL.Vertex3(x, y, z - scale);

                GL.End();

                //Draw wireframe
                GL.Color4(Color.FromArgb(200, Color.Black));
                GL.Begin(PrimitiveType.Lines);

                GL.Vertex3(x - scale, y, z);
                GL.Vertex3(x, y - scale, z);
                GL.Vertex3(x + scale, y, z);
                GL.Vertex3(x, y - scale, z);

                GL.Vertex3(x, y, z - scale);
                GL.Vertex3(x, y - scale, z);
                GL.Vertex3(x, y, z + scale);
                GL.Vertex3(x, y - scale, z);

                GL.Vertex3(x, y, z - scale);
                GL.Vertex3(x + scale, y, z);
                GL.Vertex3(x, y, z - scale);
                GL.Vertex3(x - scale, y, z);

                GL.Vertex3(x, y, z + scale);
                GL.Vertex3(x + scale, y, z);
                GL.Vertex3(x, y, z + scale);
                GL.Vertex3(x - scale, y, z);

                GL.End();
            }
        }

        public static void DrawBounds(Bounds b, Color color)
        {
            GL.LineWidth(2);

            Vector3 sPos = b.useStartPos ? b.startPos : new Vector3(0,0,0);
            
            GL.Color4(Color.FromArgb(128, color));
            GL.Begin(PrimitiveType.LineLoop);

            GL.Vertex3(b.left+sPos[0], b.top+sPos[1], 0+sPos[2]);
            GL.Vertex3(b.right+sPos[0], b.top+sPos[1], 0+sPos[2]);
            GL.Vertex3(b.right+sPos[0], b.bottom+sPos[1], 0+sPos[2]);
            GL.Vertex3(b.left+sPos[0], b.bottom+sPos[1], 0+sPos[2]);

            GL.End();
        }

        public void DrawEnemySpawners()
        {
            GL.LineWidth(2);

            foreach (EnemyGenerator c in enemySpawns)
            {
                GL.Color4(Color.FromArgb(200, Color.Fuchsia));
                foreach (EnmSection s in c.sections)
                    RenderTools.drawCubeWireframe(new Vector3(s.x,s.y,s.z), 3);
                GL.Color4(Color.FromArgb(200, Color.Yellow));
                foreach (EnmSection s in c.sections2)
                    RenderTools.drawCubeWireframe(new Vector3(s.x,s.y,s.z), 3);
            }
        }

        public void DrawItemSpawners()
        {
            foreach (ItemSpawner c in itemSpawns)
            {
                Vector3 sPos = c.useStartPos ? c.startPos : new Vector3(0,0,0);
                foreach (Section s in c.sections)
                {
                    // draw the item spawn quads
                    GL.LineWidth(2);

                    // draw outside borders
                    GL.Color3(Color.Black);

                    GL.Begin(PrimitiveType.LineStrip);
                    foreach (Vector2D vi in s.points)
                        GL.Vertex3(vi.x+sPos[0], vi.y+sPos[1], 5+sPos[2]);
                    GL.End();

                    GL.Begin(PrimitiveType.LineStrip);
                    foreach (Vector2D vi in s.points)
                        GL.Vertex3(vi.x+sPos[0], vi.y+sPos[1], -5+sPos[2]);
                    GL.End();


                    // draw vertices
                    GL.Color3(Color.White);
                    GL.Begin(PrimitiveType.Lines);
                    foreach (Vector2D vi in s.points)
                    {
                        GL.Vertex3(vi.x+sPos[0], vi.y+sPos[1], 5+sPos[2]);
                        GL.Vertex3(vi.x+sPos[0], vi.y+sPos[1], -5+sPos[2]);
                    }
                    GL.End();
                }
            }
        }

        public void DrawCollisions()
        {
            bool blink = DateTime.UtcNow.Second % 2 == 1;
            Color color;
            GL.LineWidth(4);
            Matrix4 transform = Matrix4.Identity;
            foreach (Collision c in collisions)
            {
                bool colSelected = (LVDSelection == c);
                float addX = 0, addY = 0, addZ = 0;
                if (c.useStartPos)
                {
                    addX = c.startPos[0];
                    addY = c.startPos[1];
                    addZ = c.startPos[2];
                }
                if (c.flag2)
                {
                    //Flag2 == rigged collision
                    ModelContainer riggedModel = null;
                    Bone riggedBone = null;
                    foreach (ModelContainer m in MeshList.treeView1.Nodes)
                    {
                        if (m.Text.Equals(c.subname))
                        {
                            riggedModel = m;
                            if (m.VBN != null)
                            {
                                foreach (Bone b in m.VBN.bones)
                                {
                                    if (b.Text.Equals(c.BoneName))
                                    {
                                        riggedBone = b;
                                    }
                                }
                            }
                        }
                    }
                    if (riggedModel != null)
                    {
                        if (riggedBone == null && riggedModel.VBN != null && riggedModel.VBN.bones.Count > 0)
                        {
                            riggedBone = riggedModel.VBN.bones[0];
                        }
                        if (riggedBone != null)
                            transform = riggedBone.invert * riggedBone.transform;
                    }
                }

                for (int i = 0; i < c.verts.Count - 1; i++)
                {
                    Vector3 v1Pos = Vector3.TransformPosition(new Vector3(c.verts[i].x + addX, c.verts[i].y + addY, addZ + 5), transform);
                    Vector3 v1Neg = Vector3.TransformPosition(new Vector3(c.verts[i].x + addX, c.verts[i].y + addY, addZ - 5), transform);
                    Vector3 v1Zero = Vector3.TransformPosition(new Vector3(c.verts[i].x + addX, c.verts[i].y + addY, addZ), transform);
                    Vector3 v2Pos = Vector3.TransformPosition(new Vector3(c.verts[i + 1].x + addX, c.verts[i + 1].y + addY, addZ + 5), transform);
                    Vector3 v2Neg = Vector3.TransformPosition(new Vector3(c.verts[i + 1].x + addX, c.verts[i + 1].y + addY, addZ - 5), transform);
                    Vector3 v2Zero = Vector3.TransformPosition(new Vector3(c.verts[i + 1].x + addX, c.verts[i + 1].y + addY, addZ), transform);

                    Vector3 normals = Vector3.TransformPosition(new Vector3(c.normals[i].x, c.normals[i].y, 0), transform);

                    GL.Begin(PrimitiveType.Quads);
                    if (c.normals.Count > i)
                    {
                        if (Runtime.renderCollisionNormals)
                        {
                            Vector3 v = Vector3.Add(Vector3.Divide(Vector3.Subtract(v1Zero, v2Zero), 2), v2Zero);
                            GL.End();
                            GL.Begin(PrimitiveType.Lines);
                            GL.Color3(Color.Blue);
                            GL.Vertex3(v);
                            GL.Vertex3(v.X + (c.normals[i].x * 5), v.Y + (c.normals[i].y * 5), v.Z);
                            GL.End();
                            GL.Begin(PrimitiveType.Quads);
                        }

                        float angle = (float)(Math.Atan2(normals.Y, normals.X) * 180 / Math.PI);

                        if (c.flag4)
                            color = Color.FromArgb(128, Color.Yellow);
                        else if (c.materials[i].getFlag(4) && ((angle <= 0 && angle >= -70) || (angle <= -110 && angle >= -180) || angle == 180))
                            color = Color.FromArgb(128, Color.Purple);
                        else if ((angle <= 0 && angle >= -70) || (angle <= -110 && angle >= -180) || angle == 180)
                            color = Color.FromArgb(128, Color.Lime);
                        else if (normals.Y < 0)
                            color = Color.FromArgb(128, Color.Red);
                        else
                            color = Color.FromArgb(128, Color.Cyan);

                        if ((colSelected || LVDSelection == c.normals[i]) && blink)
                            color = ColorTools.invertColor(color);

                        GL.Color4(color);
                    }
                    else
                    {
                        GL.Color4(Color.FromArgb(128, Color.Gray));
                    }
                    GL.Vertex3(v1Pos);
                    GL.Vertex3(v1Neg);
                    GL.Vertex3(v2Neg);
                    GL.Vertex3(v2Pos);
                    GL.End();

                    GL.Begin(PrimitiveType.Lines);
                    if (c.materials.Count > i)
                    {
                        if (c.materials[i].getFlag(6) || (i > 0 && c.materials[i - 1].getFlag(7)))
                            color = Color.Purple;
                        else
                            color = Color.Orange;

                        if ((colSelected || LVDSelection == c.verts[i]) && blink)
                            color = ColorTools.invertColor(color);
                        GL.Color4(color);
                    }
                    else
                    {
                        GL.Color4(Color.Gray);
                    }
                    GL.Vertex3(v1Pos);
                    GL.Vertex3(v1Neg);

                    if (i == c.verts.Count - 2)
                    {
                        if (c.materials.Count > i)
                        {
                            if (c.materials[i].getFlag(7))
                                color = Color.Purple;
                            else
                                color = Color.Orange;

                            if (LVDSelection == c.verts[i + 1] && blink)
                                color = ColorTools.invertColor(color);
                            GL.Color4(color);
                        }
                        else
                        {
                            GL.Color4(Color.Gray);
                        }
                        GL.Vertex3(v2Pos);
                        GL.Vertex3(v2Neg);
                    }
                    GL.End();
                }
                for (int i = 0; i < c.cliffs.Count; i++)
                {
                    Vector3 pos = c.cliffs[i].useStartPos ? Vector3.TransformPosition(new Vector3(c.cliffs[i].startPos.X, c.cliffs[i].startPos.Y, c.cliffs[i].startPos.Z), transform) : Vector3.TransformPosition(new Vector3(c.cliffs[i].pos.x,c.cliffs[i].pos.y,0), transform);

                    GL.Color3(Color.White);
                    GL.Begin(PrimitiveType.Lines);
                    GL.Vertex3(pos[0], pos[1], pos[2] + 10);
                    GL.Vertex3(pos[0], pos[1], pos[2] - 10);
                    GL.End();

                    GL.LineWidth(2);
                    GL.Color3(Color.Blue);
                    GL.Begin(PrimitiveType.Lines);
                    GL.Vertex3(pos);
                    GL.Vertex3(pos[0] + (c.cliffs[i].angle * 10), pos[1], pos[2]);
                    GL.End();

                    GL.LineWidth(4);
                }
            }
        }

        private static Color getLinkColor(DAT.COLL_DATA.Link link)
        {
            if ((link.flags & 1) != 0)
                return Color.FromArgb(128, Color.Yellow);
            if ((link.collisionAngle & 4) + (link.collisionAngle & 8) != 0)
                return Color.FromArgb(128, Color.Lime);
            if ((link.collisionAngle & 2) != 0)
                return Color.FromArgb(128, Color.Red);

            return Color.FromArgb(128, Color.DarkCyan);
        }

        public static void DrawDATCollisions(ModelContainer m)
        {
            float scale = m.dat_melee.stageScale;
            List<int> ledges = new List<int>();
            foreach (DAT.COLL_DATA.Link link in m.dat_melee.collisions.links)
            {

                GL.Begin(PrimitiveType.Quads);
                GL.Color4(getLinkColor(link));
                Vector2D vi = m.dat_melee.collisions.vertices[link.vertexIndices[0]];
                GL.Vertex3(vi.x * scale, vi.y * scale, 5);
                GL.Vertex3(vi.x * scale, vi.y * scale, -5);
                vi = m.dat_melee.collisions.vertices[link.vertexIndices[1]];
                GL.Vertex3(vi.x * scale, vi.y * scale, -5);
                GL.Vertex3(vi.x * scale, vi.y * scale, 5);
                GL.End();

                if ((link.flags & 2) != 0)
                {
                    ledges.Add(link.vertexIndices[0]);
                    ledges.Add(link.vertexIndices[1]);
                }
            }

            GL.LineWidth(4);
            for (int i = 0; i < m.dat_melee.collisions.vertices.Count; i++)
            {
                Vector2D vi = m.dat_melee.collisions.vertices[i];
                if (ledges.Contains(i))
                    GL.Color3(Color.Purple);
                else
                    GL.Color3(Color.Tomato);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(vi.x * scale, vi.y * scale, 5);
                GL.Vertex3(vi.x * scale, vi.y * scale, -5);
                GL.End();
            }
        }

    }

    #endregion

}



