<?xml version = "1.0" encoding = "UTF-8"?>
<xsl:stylesheet version = "1.0" xmlns:xsl = "http://www.w3.org/1999/XSL/Transform">   
	
	<xsl:template name = "style_tables"> 	

	  <style>
		table, th, td {
			border: 1px solid black;
			border-collapse: collapse;		
		}
		table {
			width: 100%;		
			border: 2px solid black;
		}
			
		
		td {
			padding: 4px;
			width: 50%;
		}
		td.row_group_1 {
			background-color: #f5f5f5;
			font-style: normal;
		}
  	  </style>
	  
	</xsl:template>
 	
</xsl:stylesheet>